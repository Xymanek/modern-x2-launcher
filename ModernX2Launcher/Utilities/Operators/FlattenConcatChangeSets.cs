using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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

            void RemoveInnerSequence(InnerSequenceTracker tracker)
            {
                _latestResult.RemoveRange(_innerSequences.IndexOf(tracker), tracker.LatestCount);
                _innerSequences.Remove(tracker);
            }

            foreach (ItemChange<InnerSequenceTracker> change in changes)
            {
                switch (change.Reason)
                {
                    case ListChangeReason.Add:
                        AddInnerSequence(change.Current, change.CurrentIndex);
                        break;

                    case ListChangeReason.Replace:
                        RemoveInnerSequence(change.Previous.Value);
                        AddInnerSequence(change.Current, change.CurrentIndex);
                        break;

                    case ListChangeReason.Remove:
                        RemoveInnerSequence(change.Current);
                        break;

                    case ListChangeReason.Refresh:
                        foreach (int innerIndex in GetResultIndices(change.Current))
                        {
                            _latestResult.RefreshAt(innerIndex);
                        }

                        break;

                    case ListChangeReason.Moved:
                        throw new NotImplementedException();

                        if (change.PreviousIndex == change.CurrentIndex) continue; // ???

                        _innerSequences.RemoveAt(change.PreviousIndex);
                        _innerSequences.Insert(change.CurrentIndex, change.Current);

                        // _latestResult.Move();
                        break;

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
            int resultOffset = GetResultOffset(tracker);

            foreach (Change<T> change in changeSet)
            {
                switch (change.Reason)
                {
                    case ListChangeReason.Add:
                        _latestResult.Insert(
                            resultOffset + change.Item.CurrentIndex,
                            change.Item.Current
                        );

                        tracker.LatestCount++;
                        break;

                    case ListChangeReason.AddRange:
                        _latestResult.AddRange(change.Range, resultOffset + change.Range.Index);

                        tracker.LatestCount += change.Range.Count;
                        break;

                    case ListChangeReason.Replace:
                        _latestResult.RemoveAt(resultOffset + change.Item.PreviousIndex);
                        _latestResult.Insert(resultOffset + change.Item.CurrentIndex, change.Item.Current);
                        break;

                    case ListChangeReason.Remove:
                        _latestResult.RemoveAt(resultOffset + change.Item.PreviousIndex);

                        tracker.LatestCount--;
                        break;

                    case ListChangeReason.RemoveRange:
                        _latestResult.RemoveRange(resultOffset + change.Range.Index, change.Range.Count);

                        tracker.LatestCount -= change.Range.Count;
                        break;

                    case ListChangeReason.Refresh:
                        _latestResult.RefreshAt(resultOffset + change.Item.CurrentIndex);
                        break;

                    case ListChangeReason.Moved:
                        throw new NotImplementedException();
                        break;

                    case ListChangeReason.Clear:
                        _latestResult.RemoveRange(resultOffset + change.Range.Index, tracker.LatestCount);

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
    }
}