using System;
using System.Collections.Generic;
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
}