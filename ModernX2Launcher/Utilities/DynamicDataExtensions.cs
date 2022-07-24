using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Collections;
using DynamicData;
using DynamicData.Binding;

namespace ModernX2Launcher.Utilities;

public static class DynamicDataExtensions
{
    /// <summary>
    /// Same as <see cref="ObservableListEx.QueryWhenChanged{TObject}"/> but guarantees that an empty snapshot will
    /// be produced if the source list is empty
    /// </summary>
    public static IObservable<IReadOnlyCollection<T>> Snapshots<T>(this IObservable<IChangeSet<T>> source)
    {
        return source
            .QueryWhenChanged()
            .Prepend(Array.Empty<T>());
    }

    public static IObservable<IChangeSet<TItem>> ToObservableChangeSet<TItem>(this AvaloniaList<TItem> collection)
    {
        return collection.ToObservableChangeSet<AvaloniaList<TItem>, TItem>();
    }

    /// <summary>
    /// Same as DynamicData's Sort, but waits until <paramref name="comparerChanged"/> has emitted a value before
    /// sorting the <paramref name="source"/>'s items instead of instantly sorting using the default comparer (which
    /// throws if the <typeparamref name="T"/> doesn't implement <see cref="IComparable"/>)
    /// </summary>
    public static IObservable<IChangeSet<T>> SortFixed<T>(
        this IObservable<IChangeSet<T>> source,
        IObservable<IComparer<T>> comparerChanged,
        SortOptions options = SortOptions.None,
        IObservable<Unit>? resort = null,
        int resetThreshold = 50
    )
    {
        return Observable.Defer(() =>
        {
            // This ensures that .Take(1) and .Skip(1) operate on the same element
            // (regardless of how the comparer sequence is setup).
            // TODO: verify that this works as intended
            IObservable<IComparer<T>> replayedComparerChanged = comparerChanged.Replay().RefCount();
            
            return replayedComparerChanged
                .Take(1)
                .SelectMany(comparerInitial => source.Sort(
                    comparerInitial,
                    options,
                    resort,
                    replayedComparerChanged.Skip(1),
                    resetThreshold
                ));
        });
    }
}