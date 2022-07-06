using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Collections;
using DynamicData.Binding;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

public partial class ModListViewModel : ViewModelBase
{
    public sealed class GroupingOption
    {
        public GroupingOption(string label, IGroupingStrategy strategy)
        {
            Label = label;
            Strategy = strategy;
        }

        public string Label { get; }
        public IGroupingStrategy Strategy { get; }
    };

    public IReadOnlyList<GroupingOption> GroupingOptions { get; }
    
    private readonly List<ModEntryViewModel> _currentDisplayedMods = new();

    private GroupingOption _selectedGroupingOption;

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

        GroupingOptions = new GroupingOption[]
        {
            new("Disabled", new DisabledGroupingStrategy()),
            new("Based on sort", new SortBasedGroupingStrategy(ModsGridCollectionView)),
            
            new("Category", new FixedPropertyGroupingStrategy(nameof(ModEntryViewModel.Category))),
            new("Author", new FixedPropertyGroupingStrategy(nameof(ModEntryViewModel.Author))),
        };

        _selectedGroupingOption = GroupingOptions[1];
        
        // This depends on _selectedGroupingOption being set
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

    public ObservableCollection<ModEntryViewModel> Mods { get; } = new();

    public DataGridCollectionView ModsGridCollectionView { get; }

    public GroupingOption SelectedGroupingOption
    {
        get => _selectedGroupingOption;
        set => this.RaiseAndSetIfChanged(ref _selectedGroupingOption, value);
    }
}