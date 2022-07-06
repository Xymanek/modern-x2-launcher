using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Data.Converters;

namespace ModernX2Launcher.ViewModels;

public partial class ModListViewModel
{
    private static readonly DataGridGroupDescription GroupingByCategory =
        new DataGridPathGroupDescription(nameof(ModEntryViewModel.Category));

    private static readonly IReadOnlyDictionary<string, DataGridGroupDescription> GroupingDescriptionsByProperty =
        new Dictionary<string, DataGridGroupDescription>
        {
            [nameof(ModEntryViewModel.Title)] = GroupingByCategory,
            [nameof(ModEntryViewModel.Category)] = GroupingByCategory,

            [nameof(ModEntryViewModel.Author)] = new DataGridPathGroupDescription(nameof(ModEntryViewModel.Author)),

            [nameof(ModEntryViewModel.IsEnabled)] =
                new DataGridPathGroupDescription(nameof(ModEntryViewModel.IsEnabled))
                {
                    ValueConverter = new FuncValueConverter<bool, string>(b => b ? "Enabled" : "Disabled")
                },
        };

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

    private class SortBasedGroupingStrategy : IGroupingStrategy
    {
        private readonly DataGridCollectionView _modsCollectionView;

        public SortBasedGroupingStrategy(DataGridCollectionView modsCollectionView)
        {
            _modsCollectionView = modsCollectionView;
        }

        /// <remarks>Since grouping derives from sorting, we don't need to manipulate the sorting</remarks>
        public ModEntrySorter? GetPrimarySorter() => null; // TODO: this needs to handle title -> category

        /// <remarks>Since grouping derives from sorting, we don't need to manipulate the sorting</remarks>
        public bool ShouldSkipSortDescription(DataGridSortDescription sortDescription) => false;

        public DataGridGroupDescription? GetGroupDescription()
        {
            // ReSharper disable once InvertIf
            if (_modsCollectionView.SortDescriptions.Count > 0)
            {
                DataGridSortDescription sortDescription = _modsCollectionView.SortDescriptions[0];

                if (
                    sortDescription.HasPropertyPath &&
                    GroupingDescriptionsByProperty.TryGetValue(
                        sortDescription.PropertyPath,
                        out DataGridGroupDescription? groupDescription
                    )
                )
                {
                    return groupDescription;
                }
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
}