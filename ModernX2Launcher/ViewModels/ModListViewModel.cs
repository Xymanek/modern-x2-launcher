using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Data.Converters;
using DynamicData.Binding;

namespace ModernX2Launcher.ViewModels;

public class ModListViewModel : ViewModelBase
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

    private readonly List<ModEntryViewModel> _currentDisplayedMods = new();

    public ModListViewModel()
    {
        Mods.Add(new ModEntryViewModel
        {
            IsEnabled = true,
            Title = "The most unique voicepack on workshop",
            Category = "Voicepacks",
            Author = "Author 3",
        });

        Mods.Add(new ModEntryViewModel
        {
            IsEnabled = false,
            Title = "LWTOC",
            Category = "Gameplay",
            Author = "Author 1",
        });

        Mods.Add(new ModEntryViewModel
        {
            IsEnabled = true,
            Title = "CI",
            Category = "Gameplay",
            Author = "Author 2",
        });

        Mods.Add(new ModEntryViewModel
        {
            IsEnabled = true,
            Title = "A boring voicepack",
            Category = "Voicepacks",
            Author = "Author 3",
        });

        ModsGridCollectionView = new DataGridCollectionView(_currentDisplayedMods, true, true);

        RebuildCurrentlyDisplayedMods();

        Observable
            .Merge(
                Mods.ObserveCollectionChanges(),
                ModsGridCollectionView.SortDescriptions.ObserveCollectionChanges()
            )
            .Subscribe(_ => RebuildCurrentlyDisplayedMods()); // TODO: dispose

        Mods.Add(new ModEntryViewModel
        {
            IsEnabled = true,
            Title = "A boring voicepack2",
            Category = "Voicepacks",
            Author = "Author 3",
        });
    }

    private void RebuildCurrentlyDisplayedMods()
    {
        IEnumerable<ModEntrySorter> GetActiveSorters()
        {
            // TODO: grouping sort

            foreach (DataGridSortDescription sortDescription in ModsGridCollectionView.SortDescriptions)
            {
                yield return ModEntrySorter.Create(
                    model => model,
                    sortDescription.Comparer,
                    false // DataGridSortDescriptions build the direction into the comparer
                );
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

            // Assume grouping by sort for now
            ModsGridCollectionView.GroupDescriptions.Clear();
            if (ModsGridCollectionView.SortDescriptions.Count > 0)
            {
                DataGridSortDescription sortDescription = ModsGridCollectionView.SortDescriptions[0];

                if (
                    sortDescription.HasPropertyPath &&
                    GroupingDescriptionsByProperty.TryGetValue(
                        sortDescription.PropertyPath,
                        out DataGridGroupDescription? groupDescription
                    )
                )
                {
                    ModsGridCollectionView.GroupDescriptions.Add(groupDescription);
                }
            }

            // Force the reset of tracking enumerator and refresh of the data grid
            // (see Avalonia.Collections.DataGridCollectionView.EnsureCollectionInSync).
            //
            // Calling ModsGridCollectionView.Refresh() here would be suboptimal as
            // that would not reset the tracking enumerator, hence likely causing
            // a second (unnecessary) refresh of the collection view later.
            _ = ModsGridCollectionView.IsEmpty;
        }
    }

    private abstract class ModEntrySorter
    {
        public abstract IOrderedEnumerable<ModEntryViewModel> Apply(IEnumerable<ModEntryViewModel> mods);

        public static ModEntrySorter<TKey> Create<TKey>(
            Func<ModEntryViewModel, TKey> keySelector, bool descending
        )
        {
            return Create(keySelector, null, descending);
        }

        public static ModEntrySorter<TKey> Create<TKey>(
            Func<ModEntryViewModel, TKey> keySelector, IComparer<TKey>? comparer, bool descending
        )
        {
            return new ModEntrySorter<TKey>(keySelector, comparer, descending);
        }
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

    public ObservableCollection<ModEntryViewModel> Mods { get; } = new();

    public DataGridCollectionView ModsGridCollectionView { get; }
}