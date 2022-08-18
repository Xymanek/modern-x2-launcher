using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DynamicData;

namespace ModernX2Launcher.Utilities.Operators;

internal class FlattenConcatChangeSets<T>
{
    private readonly IObservable<IChangeSet<IObservable<IChangeSet<T>>>> _source;

    public FlattenConcatChangeSets(IObservable<IChangeSet<IObservable<IChangeSet<T>>>> source)
    {
        _source = source;
    }

    public IObservable<IChangeSet<T>> Run()
    {
        return Observable.Create<IChangeSet<T>>(
            observer => new Processor(this, observer).Subscribe()
        );
    }

    private sealed class Processor
    {
        private readonly FlattenConcatChangeSets<T> _definition;
        private readonly IObserver<IChangeSet<T>> _observer;

        private readonly object _locker = new();
        private readonly List<InnerSequenceTracker> _innerSequences = new();

        private readonly ChangeAwareList<T> _latestResult = new();

        public Processor(FlattenConcatChangeSets<T> definition, IObserver<IChangeSet<T>> observer)
        {
            _definition = definition;
            _observer = observer;
        }

        public IDisposable Subscribe()
        {
            IObservable<IChangeSet<InnerSequenceTracker>> transformedSource = _definition._source
                // TODO: this happens twice for a single add so the tracker in ProcessInnerChangeSet
                // is not the same as the one in _innerSequences
                .Transform(innerSequence => new InnerSequenceTracker(innerSequence))
                .Synchronize(_locker);

            IObservable<IChangeSet<T>> outerResults = transformedSource
                .Select(ProcessOuterChangeSet);

            IObservable<IChangeSet<T>> innerResults = transformedSource
                .MergeMany(
                    sequenceTracker => sequenceTracker.Sequence
                        .Select(changeSet => (Tracker: sequenceTracker, Changes: changeSet))
                )
                .Select(tuple => ProcessInnerChangeSet(tuple.Tracker, tuple.Changes));

            // Important: outerResults (ProcessOuterChangeSet) must be processed before the associated innerResults
            // (ProcessInnerChangeSet), else the inner won't know where they fit relative to the other sequences
            return outerResults.Merge(innerResults).SubscribeSafe(_observer);
        }

        private IChangeSet<T> ProcessOuterChangeSet(IChangeSet<InnerSequenceTracker> changeSet)
        {
            // I don't expect batch changes of sequences and this makes the following code simpler
            IEnumerable<ItemChange<InnerSequenceTracker>> changes = changeSet.Flatten();

            void AddInnerSequence(InnerSequenceTracker tracker, int index)
            {
                if (tracker.LatestCount != 0)
                {
                    throw new InvalidOperationException(
                        $"Adding {nameof(InnerSequenceTracker)} but {nameof(InnerSequenceTracker.LatestCount)} is not 0"
                    );
                }

                _innerSequences.Insert(index, tracker);
            }

            void RemoveInnerSequence(int index)
            {
                InnerSequenceTracker tracker = _innerSequences[index];
                
                _latestResult.RemoveRange(index, tracker.LatestCount);
                _innerSequences.RemoveAt(index);
            }

            foreach (ItemChange<InnerSequenceTracker> change in changes)
            {
                switch (change.Reason)
                {
                    case ListChangeReason.Add:
                        Ensure(change.CurrentIndex > -1);
                        
                        AddInnerSequence(change.Current, change.CurrentIndex);
                        break;

                    case ListChangeReason.Replace:
                        Ensure(change.Previous.HasValue);
                        Ensure(change.CurrentIndex > -1);

                        RemoveInnerSequence(change.PreviousIndex);
                        AddInnerSequence(change.Current, change.CurrentIndex);
                        break;

                    case ListChangeReason.Remove:
                        RemoveInnerSequence(change.CurrentIndex);
                        break;

                    case ListChangeReason.Refresh:
                        foreach (int innerIndex in GetResultIndices(change.Current))
                        {
                            _latestResult.RefreshAt(innerIndex);
                        }

                        break;

                    case ListChangeReason.Moved:
                        // I currently don't need this and I'm too lazy to figure out the index math
                        throw new NotImplementedException();

                        /*if (change.PreviousIndex == change.CurrentIndex) continue; // ???

                        _innerSequences.RemoveAt(change.PreviousIndex);
                        _innerSequences.Insert(change.CurrentIndex, change.Current);

                        // _latestResult.Move();
                        break;*/

                    case ListChangeReason.Clear:
                    case ListChangeReason.AddRange:
                    case ListChangeReason.RemoveRange:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return _latestResult.CaptureChanges();
        }

        private IChangeSet<T> ProcessInnerChangeSet(
            InnerSequenceTracker tracker, IChangeSet<T> changeSet
        )
        {
            Ensure(_innerSequences.Contains(tracker));
            
            int resultOffset = GetResultOffset(tracker);

            foreach (Change<T> change in changeSet)
            {
                switch (change.Reason)
                {
                    case ListChangeReason.Add:
                        Ensure(change.Item.CurrentIndex > -1);

                        _latestResult.Insert(
                            resultOffset + change.Item.CurrentIndex,
                            change.Item.Current
                        );

                        tracker.LatestCount++;
                        break;

                    case ListChangeReason.AddRange:
                        Ensure(change.Range.Index > -1);

                        _latestResult.AddRange(change.Range, resultOffset + change.Range.Index);

                        tracker.LatestCount += change.Range.Count;
                        break;

                    case ListChangeReason.Replace:
                        Ensure(change.Item.CurrentIndex > -1);
                        Ensure(change.Item.PreviousIndex > -1);

                        _latestResult.RemoveAt(resultOffset + change.Item.PreviousIndex);
                        _latestResult.Insert(resultOffset + change.Item.CurrentIndex, change.Item.Current);
                        break;

                    case ListChangeReason.Remove:
                        Ensure(change.Item.PreviousIndex > -1);

                        _latestResult.RemoveAt(resultOffset + change.Item.PreviousIndex);

                        tracker.LatestCount--;
                        break;

                    case ListChangeReason.RemoveRange:
                        Ensure(change.Range.Index > -1);

                        _latestResult.RemoveRange(resultOffset + change.Range.Index, change.Range.Count);

                        tracker.LatestCount -= change.Range.Count;
                        break;

                    case ListChangeReason.Refresh:
                        Ensure(change.Item.CurrentIndex > -1);

                        _latestResult.RefreshAt(resultOffset + change.Item.CurrentIndex);
                        break;

                    case ListChangeReason.Moved:
                        Ensure(change.Item.CurrentIndex > -1);
                        Ensure(change.Item.PreviousIndex > -1);
                        
                        _latestResult.Move(
                            resultOffset + change.Item.PreviousIndex,
                            resultOffset + change.Item.CurrentIndex
                        );
                        break;

                    case ListChangeReason.Clear:
                        _latestResult.RemoveRange(resultOffset, tracker.LatestCount);

                        tracker.LatestCount = 0;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return _latestResult.CaptureChanges();
        }

        private IEnumerable<int> GetResultIndices(InnerSequenceTracker tracker)
        {
            return Enumerable.Range(
                GetResultOffset(tracker),
                tracker.LatestCount
            );
        }

        private int GetResultOffset(InnerSequenceTracker tracker)
        {
            return _innerSequences
                .TakeWhile(t => t != tracker)
                .Sum(t => t.LatestCount);
        }

        private sealed class InnerSequenceTracker
        {
            public readonly IObservable<IChangeSet<T>> Sequence;

            private int _latestCount;

            public InnerSequenceTracker(IObservable<IChangeSet<T>> sequence)
            {
                Sequence = sequence;
            }

            /// <summary>
            /// The number of elements that the source collection contains, based on the changes so far
            /// </summary>
            public int LatestCount
            {
                get => _latestCount;
                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(value), value,
                            $"{nameof(LatestCount)} cannot be less than 0"
                        );
                    }

                    _latestCount = value;
                }
            }
        }

        private void Ensure(bool condition, [CallerArgumentExpression("condition")] string expression = default!)
        {
            if (!condition)
            {
                throw new Exception("Ensure failed: " + expression);
            }
        }
    }
}