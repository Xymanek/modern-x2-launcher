using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Data.Converters;

namespace ModernX2Launcher.ViewModels;

public partial class ModListViewModel
{
    private void RebuildCurrentlyDisplayedMods()
    {
        IEnumerable<ModEntrySorter> GetActiveSorters()
        {
            ModEntrySorter? primarySorter = SelectedGroupingOption.Strategy.GetPrimarySorter();

            if (primarySorter != null)
            {
                yield return primarySorter;
            }

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (DataGridSortDescription sortDescription in ModsGridCollectionView.SortDescriptions)
            {
                if (SelectedGroupingOption.Strategy.ShouldSkipSortDescription(sortDescription)) continue;

                yield return CreateSorterFromSortDescription(sortDescription);
            }
        }

        IEnumerable<ModEntryViewModel> newModsSequence = Mods;

        // ReSharper disable once LoopCanBeConvertedToQuery - ugly cuz cannot infer generic args
        foreach (ModEntrySorter sorter in GetActiveSorters())
        {
            newModsSequence = sorter.Apply(newModsSequence);
        }

        using (ModsGridCollectionView.DeferRefresh())
        {
            _currentDisplayedMods.Clear();
            _currentDisplayedMods.AddRange(newModsSequence);

            ModsGridCollectionView.GroupDescriptions.Clear();
            
            DataGridGroupDescription? desiredGrouping = SelectedGroupingOption.Strategy.GetGroupDescription();
            if (desiredGrouping != null) ModsGridCollectionView.GroupDescriptions.Add(desiredGrouping);

            // Force the reset of tracking enumerator and refresh of the data grid
            // (see Avalonia.Collections.DataGridCollectionView.EnsureCollectionInSync).
            //
            // Calling ModsGridCollectionView.Refresh() here would be suboptimal as
            // that would not reset the tracking enumerator, hence likely causing
            // a second (unnecessary) refresh of the collection view later.
            _ = ModsGridCollectionView.IsEmpty;
        }
    }

    public abstract class ModEntrySorter
    {
        public abstract IOrderedEnumerable<ModEntryViewModel> Apply(IEnumerable<ModEntryViewModel> mods);
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

        public override IOrderedEnumerable<ModEntryViewModel> Apply(IEnumerable<ModEntryViewModel> mods)
        {
            if (mods is IOrderedEnumerable<ModEntryViewModel> orderedEnumerable)
            {
                return orderedEnumerable.CreateOrderedEnumerable(_keySelector, _comparer, _descending);
            }

            return _descending
                ? mods.OrderByDescending(_keySelector, _comparer)
                : mods.OrderBy(_keySelector, _comparer);
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
        return CreateSorter(
            model => model,
            sortDescription.Comparer,
            false // DataGridSortDescriptions build the direction into the comparer
        );
    }

    public interface IGroupingStrategy
    {
        ModEntrySorter? GetPrimarySorter();

        bool ShouldSkipSortDescription(DataGridSortDescription sortDescription);

        DataGridGroupDescription? GetGroupDescription();
    }

    private class DisabledGroupingStrategy : IGroupingStrategy
    {
        public ModEntrySorter? GetPrimarySorter() => null;

        public bool ShouldSkipSortDescription(DataGridSortDescription sortDescription) => false;

        public DataGridGroupDescription? GetGroupDescription() => null;
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
                [nameof(ModEntryViewModel.Title)] = CreateSorter(model => model.Category, false),
            };

        private readonly DataGridCollectionView _modsCollectionView;

        public SortBasedGroupingStrategy(DataGridCollectionView modsCollectionView)
        {
            _modsCollectionView = modsCollectionView;
        }

        private DataGridSortDescription? GetFirstSortDescription()
            => _modsCollectionView.SortDescriptions.FirstOrDefault();

        public ModEntrySorter? GetPrimarySorter()
        {
            DataGridSortDescription? sortDescription = GetFirstSortDescription();

            if (
                sortDescription is { HasPropertyPath: true } &&
                PrependedSortersByProperty.TryGetValue(sortDescription.PropertyPath, out ModEntrySorter? sorter)
            )
            {
                return sorter;
            }

            return null;
        }

        /// <remarks>Even if we prepend a different grouping sort, we still need to keep the column intact</remarks>
        public bool ShouldSkipSortDescription(DataGridSortDescription sortDescription) => false;

        public DataGridGroupDescription? GetGroupDescription()
        {
            DataGridSortDescription? sortDescription = GetFirstSortDescription();

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
        }
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

        public ModEntrySorter GetPrimarySorter() => _sorter;

        public bool ShouldSkipSortDescription(DataGridSortDescription sortDescription)
            => sortDescription.HasPropertyPath && sortDescription.PropertyPath == _propertyPath;

        public DataGridGroupDescription? GetGroupDescription()
        {
            GroupingDescriptionsByProperty.TryGetValue(_propertyPath, out DataGridGroupDescription? groupDescription);

            return groupDescription;
        }
    }
}