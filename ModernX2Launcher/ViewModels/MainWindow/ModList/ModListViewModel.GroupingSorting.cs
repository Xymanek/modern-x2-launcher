using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Data.Converters;
using DynamicData.Binding;
using ModernX2Launcher.Utilities;
using ModernX2Launcher.ViewModels.Common;

namespace ModernX2Launcher.ViewModels.MainWindow.ModList;

public partial class ModListViewModel
{
    public abstract class ModEntrySorter
    {
        public abstract IComparer<ModEntryViewModel> AsComparer();

        // Should be init only but rn that would force passing too many args to CreateSorter, so eh.
        // This needs a better API in general.
        public Func<ModEntryViewModel, IObservable<Unit>>? ResortObservableProvider { get; set; }
    }

    private class ModEntrySorter<TKey> : ModEntrySorter
    {
        private readonly Func<ModEntryViewModel, TKey> _keySelector;
        private readonly IComparer<TKey>? _comparer;
        private readonly bool _descending;

        public ModEntrySorter(Func<ModEntryViewModel, TKey> keySelector, IComparer<TKey>? comparer, bool descending)
        {
            _keySelector = keySelector;
            _comparer = comparer;
            _descending = descending;
        }

        public override IComparer<ModEntryViewModel> AsComparer()
        {
            return Comparer<ModEntryViewModel>.Create((x, y) =>
            {
                TKey xKey = _keySelector(x);
                TKey yKey = _keySelector(y);

                int result = (_comparer ?? Comparer<TKey>.Default).Compare(xKey, yKey);
                if (_descending) result *= -1;

                return result;
            });
        }
    }

    private static ModEntrySorter<TKey> CreateSorter<TKey>(
        Func<ModEntryViewModel, TKey> keySelector, bool descending
    )
    {
        return CreateSorter(keySelector, null, descending);
    }

    private static ModEntrySorter<TKey> CreateSorter<TKey>(
        Func<ModEntryViewModel, TKey> keySelector, IComparer<TKey>? comparer, bool descending
    )
    {
        return new ModEntrySorter<TKey>(keySelector, comparer, descending);
    }

    private static ModEntrySorter CreateSorterFromSortDescription(DataGridSortDescription sortDescription)
    {
        ModEntrySorter sorter = CreateSorter(
            model => model,
            sortDescription.Comparer,
            false // DataGridSortDescriptions build the direction into the comparer
        );

        if (sortDescription.HasPropertyPath)
        {
            sorter.ResortObservableProvider = model => model
                .WhenAnyPropertyChanged(sortDescription.PropertyPath)
                .Select(_ => Unit.Default);
        }

        return sorter;
    }

    public interface IGroupingStrategy
    {
        IObservable<DataGridGroupDescription?> GroupDescriptionObs { get; }

        IObservable<ModEntrySorter?> PrimarySorterObs { get; }

        IObservable<bool> ShouldSkipSortDescription(DataGridSortDescription sortDescription);
    }

    private class DisabledGroupingStrategy : IGroupingStrategy
    {
        public IObservable<DataGridGroupDescription?> GroupDescriptionObs { get; }
            = Observable.Return<DataGridGroupDescription?>(null);

        public IObservable<ModEntrySorter?> PrimarySorterObs { get; } = Observable.Return<ModEntrySorter?>(null);

        public IObservable<bool> ShouldSkipSortDescription(DataGridSortDescription sortDescription)
            => Observable.Return(false);
    }

    private static readonly DataGridGroupDescription GroupingByCategory =
        new DataGridPathGroupDescription(nameof(ModEntryViewModel.Category));

    private static readonly IReadOnlyDictionary<string, DataGridGroupDescription> GroupingDescriptionsByProperty =
        new Dictionary<string, DataGridGroupDescription>
        {
            // TODO: This duplicates stuff in SortBasedGroupingStrategy and doesn't actually solve
            // "sorting/grouping for some columns is done by proxy value" (e.g. dates).
            // Should ideally be reworked.
            [nameof(ModEntryViewModel.Title)] = GroupingByCategory,
            [nameof(ModEntryViewModel.Category)] = GroupingByCategory,

            [nameof(ModEntryViewModel.Author)] = new DataGridPathGroupDescription(nameof(ModEntryViewModel.Author)),

            [nameof(ModEntryViewModel.IsEnabled)] =
                new DataGridPathGroupDescription(nameof(ModEntryViewModel.IsEnabled))
                {
                    // TODO: This is not really optimal. First, we convert from bool to string, then data grid
                    // compares strings. A better approach would be fixing the group header row template to
                    // display custom things
                    ValueConverter = new FuncValueConverter<bool, string>(b => b ? "Enabled" : "Disabled")
                },
        };

    private class SortBasedGroupingStrategy : IGroupingStrategy
    {
        private static readonly IReadOnlyDictionary<string, ModEntrySorter> PrependedSortersByProperty
            = new Dictionary<string, ModEntrySorter>
            {
                [nameof(ModEntryViewModel.Title)] = CreateSorter(model => model.Category, false), // TODO resort provider
            };

        private readonly IObservable<DataGridSortDescription?> _firstSortDescriptionObs;

        public SortBasedGroupingStrategy(DataGridCollectionView modsCollectionView)
        {
            _firstSortDescriptionObs = modsCollectionView.SortDescriptions
                .ToObservableChangeSet()
                .Snapshots()
                .SelectFirstOrDefault();
        }

        public IObservable<DataGridGroupDescription?> GroupDescriptionObs
            => _firstSortDescriptionObs
                .Select(sortDescription =>
                {
                    if (
                        sortDescription is { HasPropertyPath: true } &&
                        GroupingDescriptionsByProperty.TryGetValue(
                            sortDescription.PropertyPath,
                            out DataGridGroupDescription? groupDescription
                        )
                    )
                    {
                        return groupDescription;
                    }

                    return null;
                });

        public IObservable<ModEntrySorter?> PrimarySorterObs
            => _firstSortDescriptionObs
                .Select(description =>
                {
                    if (
                        description is { HasPropertyPath: true } &&
                        PrependedSortersByProperty.TryGetValue(description.PropertyPath, out ModEntrySorter? sorter)
                    )
                    {
                        return sorter;
                    }

                    return null;
                });

        /// <remarks>Even if we prepend a different grouping sort, we still need to keep the column intact</remarks>
        public IObservable<bool> ShouldSkipSortDescription(DataGridSortDescription sortDescription)
            => Observable.Return(false);
    }

    private class FixedPropertyGroupingStrategy : IGroupingStrategy
    {
        private readonly string _propertyPath;
        private readonly ModEntrySorter _sorter;

        public FixedPropertyGroupingStrategy(string propertyPath)
        {
            _propertyPath = propertyPath;
            _sorter = CreateSorterFromSortDescription(DataGridSortDescription.FromPath(_propertyPath));
        }

        public IObservable<DataGridGroupDescription?> GroupDescriptionObs
        {
            get
            {
                GroupingDescriptionsByProperty.TryGetValue(
                    _propertyPath, out DataGridGroupDescription? groupDescription
                );

                return Observable.Return(groupDescription);
            }
        }

        public IObservable<ModEntrySorter?> PrimarySorterObs => Observable.Return(_sorter);

        public IObservable<bool> ShouldSkipSortDescription(DataGridSortDescription sortDescription)
            => Observable.Return(sortDescription.HasPropertyPath && sortDescription.PropertyPath == _propertyPath);
    }
}