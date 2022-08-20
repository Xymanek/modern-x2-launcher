using DynamicData;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace ModernX2Launcher.Utilities.Operators;

internal sealed class FlattenConcatChangeSets2<T>
{
    private readonly IObservable<IChangeSet<IObservable<IChangeSet<T>>>> _source;

    public FlattenConcatChangeSets2(IObservable<IChangeSet<IObservable<IChangeSet<T>>>> source)
    {
        _source = source;
    }

    public IObservable<IChangeSet<T>> Run()
    {
        return Observable.Create<IChangeSet<T>>(observer =>
        {
            Processor processor = new(this, observer);
            processor.Subscribe();

            return processor;
        });
    }

    // This should be a Sink<,> but that's internal so just replicating the code
    private sealed class Processor : IDisposable
    {
        private readonly FlattenConcatChangeSets2<T> _definition;
        private readonly IObserver<IChangeSet<T>> _observer;

        private readonly SingleAssignmentDisposable _upstream = new();
        private bool _disposed;

        private bool _isUpstreamCompleted;

        private readonly object _locker = new();

        private readonly List<InnerObservableTracker> _innerObservables = new();
        private readonly ChangeAwareList<T> _latestResult = new();

        public Processor(FlattenConcatChangeSets2<T> definition, IObserver<IChangeSet<T>> observer)
        {
            _definition = definition;
            _observer = observer;
        }

        public void Subscribe()
        {
            var observer = Observer.Create<IChangeSet<IObservable<IChangeSet<T>>>>(
                changeSet =>
                {
                    try
                    {
                        ProcessOuterChangeSet(changeSet);
                    }
                    catch (Exception e)
                    {
                        ForwardOnError(e);
                    }
                },
                ForwardOnError,
                OnOuterCompleted
            );
            observer = Observer.Synchronize(observer, _locker); // I wish this was an extension...

            _upstream.Disposable = _definition._source.SubscribeSafe(observer);
        }

        private void ForwardOnNext(IChangeSet<T> value)
        {
            _observer.OnNext(value);
        }

        private void ForwardChanges()
        {
            ForwardOnNext(_latestResult.CaptureChanges());
        }

        private void ForwardOnCompleted()
        {
            _observer.OnCompleted();
            Dispose();
        }

        private void ForwardOnError(Exception error)
        {
            _observer.OnError(error);
            Dispose();

            // Note: Merge and CombineLatest don't self-dispose when getting an error.
            // In our case we explicitly want to, since otherwise there is guarantee that
            // the information about the state of upstream collections is still valid
            // (and we don't want to feed garbage data downstream).
        }

        private void ProcessOuterChangeSet(IChangeSet<IObservable<IChangeSet<T>>> changeSet)
        {
            // TODO

            ForwardChanges();
        }

        private void ProcessInnerChangeSet(InnerObservableTracker tracker, IChangeSet<T> changeSet)
        {
            // TODO

            ForwardChanges();
        }

        private void OnOuterCompleted()
        {
            _isUpstreamCompleted = true;
            OnInnerCompleted();
        }

        private void OnInnerCompleted()
        {
            // Forward completed only if outer and all inners are completed

            if (!_isUpstreamCompleted) return;

            // I'm too tired to get the correct LINQ call for this -.-
            // (note - this check must be skipped if there are no trackers)
            foreach (InnerObservableTracker tracker in _innerObservables)
            {
                if (!tracker.IsCompleted) return;
            }

            ForwardOnCompleted();
        }

        private sealed class InnerObservableTracker
        {
            private readonly Processor _parent;

            private readonly SingleAssignmentDisposable _upstream = new();

            private int _latestCount;

            public InnerObservableTracker(Processor parent)
            {
                _parent = parent;
            }

            public void SubscribeUpstream(IObservable<IChangeSet<T>> innerObservable)
            {
                var observer = Observer.Create<IChangeSet<T>>(
                    changeSet =>
                    {
                        try
                        {
                            _parent.ProcessInnerChangeSet(this, changeSet);
                        }
                        catch (Exception e)
                        {
                            _parent.ForwardOnError(e);
                        }
                    },
                    _parent.ForwardOnError,
                    OnCompleted
                );
                observer = Observer.Synchronize(observer, _parent._locker);

                _upstream.Disposable = innerObservable.SubscribeSafe(observer);
            }

            private void OnCompleted()
            {
                IsCompleted = true;
                _parent.OnInnerCompleted();
            }

            /// <summary>
            /// The number of elements that the source collection contains, based on the changes so far
            /// </summary>
            public int LatestCount
            {
                get => _latestCount;
                set
                {
                    Ensure(value >= 0);

                    _latestCount = value;
                }
            }

            public bool IsCompleted { get; private set; }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _upstream.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposed = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Processor()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    private static void Ensure(
        bool condition,
        [CallerArgumentExpression("condition")] string expression = default!
    )
    {
        if (!condition)
        {
            throw new Exception("Ensure failed: " + expression);
        }
    }
}