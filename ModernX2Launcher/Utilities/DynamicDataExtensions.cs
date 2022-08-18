using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Collections;
using DynamicData;
using DynamicData.Binding;
using ModernX2Launcher.Utilities.Operators;

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
        return Observable.Create<IChangeSet<T>>(observer =>
        {
            CompositeDisposable disposables = new();

            // This ensures that .Take(1) and .Skip(1) operate on the same element
            // (regardless of how the comparer sequence is setup).
            IConnectableObservable<IComparer<T>> replayedComparerChanged = comparerChanged.Replay();

            // We are being subscribed to, so also subscribe to the comparerChanged
            replayedComparerChanged.Connect()

                // When we are unsubscribed from, we need to also stop buffering the comparerChanged emits
                .DisposeWith(disposables);

            replayedComparerChanged
                .Take(1)

                // Due to Take(1), it doesn't matter if we use SelectMany() or Select().Switch() here
                // (using SelectMany since Switch's implementation is more complicated)
                .SelectMany(comparerInitial => source.Sort(
                    comparerInitial,
                    options,
                    resort,
                    replayedComparerChanged.Skip(1),
                    resetThreshold
                ))
                .SubscribeSafe(observer)
                .DisposeWith(disposables);

            return disposables;
        });
    }

    // IChangeSet isn't variant so we gotta do this atrocity that also fails type inference
    public static IObservable<IChangeSet<TItem>> FlattenConcat<TInnerCollection, TItem>(
        this IObservable<IChangeSet<TInnerCollection>> source
    )
        where TInnerCollection : IObservableList<TItem>
    {
        return source
            .Transform(static innerList => innerList.Connect())
            .FlattenConcat();
    }

    /*public static IObservable<IChangeSet<T>> FlattenConcat<T>(
        this IObservable<IChangeSet<IObservableList<T>>> source
    )
    {
        return source
            .Transform(static innerList => innerList.Connect())
            .FlattenConcat();
    }*/

    public static IObservable<IChangeSet<T>> FlattenConcat<T>(
        this IObservable<IChangeSet<IObservable<IChangeSet<T>>>> source
    )
    {
        return new FlattenConcatChangeSets<T>(source).Run();
    }
}