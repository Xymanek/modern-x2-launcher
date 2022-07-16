using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;

namespace ModernX2Launcher.Utilities.SortStack;

/// <summary>
/// Represents a strategy by which a sequence will be sorted
/// </summary>
/// <typeparam name="TEntry">Type of items in the sequence</typeparam>
public interface ISorter<TEntry>
{
    IOrderedEnumerable<TEntry> SortEnumerable(IEnumerable<TEntry> enumerable);
}

public interface IBidirectionalSorter<TEntry> : ISorter<TEntry>
{
    bool IsDescending { get; }

    IBidirectionalSorter<TEntry> GetReversed();
}

// TODO: better name
public interface IRefreshableSorter<TEntry> : ISorter<TEntry>
{
    /// <returns>
    /// An observable that emits when a value that was used in comparison has changed,
    /// thus the sorting should be re-applied.
    /// </returns>
    /// <remarks>The returned observable must NOT emit on subscription</remarks>
    IObservable<Unit> GetResortObservable(TEntry entry);
}