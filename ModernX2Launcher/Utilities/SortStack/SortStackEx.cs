using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace ModernX2Launcher.Utilities.SortStack;

public static class SortStackEx
{
    public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source, IEnumerable<ISorter<T>> sorters)
    {
        ISorter<T>[] sortersMaterialized = sorters as ISorter<T>[] ?? sorters.ToArray();
        
        if (!sortersMaterialized.Any())
        {
            throw new ArgumentException("Cannot order by no sorters", nameof(sorters));
        }
        
        IOrderedEnumerable<T>? orderedEnumerable = null;

        // ReSharper disable once LoopCanBeConvertedToQuery - ugly because cannot infer types
        foreach (ISorter<T> sorter in sortersMaterialized)
        {
            orderedEnumerable = sorter.SortEnumerable(orderedEnumerable ?? source);
        }

        return orderedEnumerable!;
    }

    public static IObservable<Unit> ToResortObservable<T>(
        this IEnumerable<T> source, IEnumerable<IRefreshableSorter<T>> sorters
    )
    {
        return source
            .SelectMany(item => sorters.Select(sorter => sorter.GetResortObservable(item)))
            .Merge();
    }
}