using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Collections;
using DynamicData.Binding;

namespace ModernX2Launcher.Utilities.SortStack;

public class DataGridDescriptionSorter<TEntry> :
    SorterBase<TEntry, TEntry>, // Not bidirectional base as the _description.Comparer already includes the direction 
    IBidirectionalSorter<TEntry>
{
    private readonly DataGridSortDescription _description;
    private readonly Comparer<TEntry> _comparer;

    public DataGridDescriptionSorter(DataGridSortDescription description)
    {
        _description = description;
        _comparer = Comparer<TEntry>.Create((x, y) => _description.Comparer.Compare(x, y));
    }

    protected override TEntry GetSortKey(TEntry entry) => entry;

    protected override IComparer<TEntry> GetComparer() => _comparer;

    public bool IsDescending => _description.Direction == ListSortDirection.Descending;

    public IBidirectionalSorter<TEntry> GetReversed()
    {
        return new DataGridDescriptionSorter<TEntry>(_description.SwitchSortDirection());
    }
}

public class RefreshableDataGridDescriptionSorter<TEntry> 
    : DataGridDescriptionSorter<TEntry>, IRefreshableSorter<TEntry>
    where TEntry : INotifyPropertyChanged
{
    private readonly DataGridSortDescription _description;

    public RefreshableDataGridDescriptionSorter(DataGridSortDescription description) : base(description)
    {
        if (!description.HasPropertyPath)
        {
            throw new ArgumentException(
                $"Only {nameof(DataGridSortDescription)}s with property path are supported", nameof(description)
            );
        }

        _description = description;

        if (description.PropertyPath.Contains('.'))
        {
            throw new NotSupportedException("Nested property is currently not supported");
        }
    }

    public IObservable<Unit> GetResortObservable(TEntry entry)
    {
        return entry.WhenAnyPropertyChanged(_description.PropertyPath)
            .Select(_ => Unit.Default);
    }
}