using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

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
}