using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ModernX2Launcher.Utilities;

public static class ObservableExtensions
{
    public static IObservable<T?> SelectFirstOrDefault<T>(this IObservable<IEnumerable<T>> source)
        => source.Select(enumerable => enumerable.FirstOrDefault());

    /// <summary>
    /// Applies <paramref name="eligibilityProvider"/> to each item of every emit of <paramref name="source"/>
    /// and re-emits the collection with the ineligible items removed every time the eligibility observers emit
    /// a different value.
    ///
    /// Each emit of <paramref name="source"/> restarts the filtering pipeline
    /// (<see cref="Observable.Switch{TSource}(System.IObservable{System.IObservable{TSource}})"/>)
    /// </summary>
    public static IObservable<IReadOnlyList<T>> FilterEach<T>(
        this IObservable<IEnumerable<T>> source,
        Func<T, IObservable<bool>> eligibilityProvider
    )
    {
        return source
            .Select(enumerable =>
            {
                return enumerable
                    .Select(innerValue =>
                    {
                        return eligibilityProvider(innerValue)
                            .Select(isEligible => (innerValue, isEligible))
                            .DistinctUntilChanged(tuple => tuple.isEligible);
                    })
                    .CombineLatest()
                    .Select(valuesWithEligibility =>
                    {
                        return (IReadOnlyList<T>)valuesWithEligibility
                            .Where(tuple => tuple.isEligible)
                            .Select(tuple => tuple.innerValue)
                            .ToArray();
                    });
            })
            .Switch();
    }

    public static IObservable<IReadOnlyList<TInnerResult>> Transform<TInnerSource, TInnerResult>(
        this IObservable<IEnumerable<TInnerSource>> source,
        Func<TInnerSource, TInnerResult> selector
    )
    {
        return source.Select(enumerable => enumerable.Select(selector).ToArray());
    }

    // TODO: unit testing support
    /// <summary>
    /// Will throttle until the next UI tick. Useful when a certain action (e.g. button press) causes multiple
    /// emits to fire synchronously (e.g. due to complex observable pipeline) but only the final one
    /// is required to be acted upon.
    /// </summary>
    /// <remarks>
    /// Technically the latest throttled value will be propagated when the dispatcher processes the
    /// <see cref="Avalonia.Threading.DispatcherPriority.DataBind"/> jobs, which can happen during the current
    /// tick if we are currently processing higher priority jobs.
    /// </remarks>
    public static IObservable<T> ThrottlePerTick<T>(this IObservable<T> source)
        => source.Throttle(TimeSpan.Zero, NotInlineAvaloniaScheduler.Instance);

    // I'm lazy, will implement other Throttle overloads as necessary
    public static IObservable<TSource> ThrottleExcludingFirst<TSource, TThrottle>(
        this IObservable<TSource> source, Func<TSource, IObservable<TThrottle>> throttleDurationSelector
    )
    {
        return Observable.Create<TSource>(observer =>
        {
            CompositeDisposable disposables = new();

            IConnectableObservable<TSource> replayedSource = source.Replay();
            replayedSource.Connect().DisposeWith(disposables);

            replayedSource
                .Skip(1)
                .Throttle(throttleDurationSelector)
                .Merge(replayedSource.Take(1))
                .SubscribeSafe(observer)
                .DisposeWith(disposables);

            return disposables;
        });
    }
}