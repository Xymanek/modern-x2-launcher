using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Collections;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

public partial class ModListViewModel : ViewModelBase
{
    public sealed class GroupingOption
    {
        public GroupingOption(
            string label, IGroupingStrategy strategy,
            IObservable<GroupingOption> activeOption
        )
        {
            Label = label;
            Strategy = strategy;
            
            IsActive = activeOption.Select(option => option == this);

            Selected = ReactiveCommand.Create(
                () => this,
                IsActive.Select(isActive => !isActive)
            );
        }

        public string Label { get; }
        public IGroupingStrategy Strategy { get; }
        
        public ReactiveCommand<Unit, GroupingOption> Selected { get; }

        public IObservable<bool> IsActive { get; }
    }

    public IReadOnlyList<GroupingOption> GroupingOptions { get; }
    
    private readonly List<ModEntryViewModel> _currentDisplayedMods = new();

    private GroupingOption _selectedGroupingOption;

    public ModListViewModel()
    {
        ModsGridCollectionView = new DataGridCollectionView(_currentDisplayedMods, true, true);
        ModsGridCollectionView.SortDescriptions.Add(
            DataGridSortDescription.FromPath(nameof(ModEntryViewModel.Title))
        );

        IObservable<GroupingOption> activeGrouping = this.WhenAnyValue(m => m.SelectedGroupingOption);

        GroupingOptions = new GroupingOption[]
        {
            new("Disabled", new DisabledGroupingStrategy(), activeGrouping),
            new("Based on sort", new SortBasedGroupingStrategy(ModsGridCollectionView), activeGrouping),
            
            new("Category", new FixedPropertyGroupingStrategy(nameof(ModEntryViewModel.Category)), activeGrouping),
            new("Author", new FixedPropertyGroupingStrategy(nameof(ModEntryViewModel.Author)), activeGrouping),
        };

        _selectedGroupingOption = GroupingOptions[1];
        
        // We need to raise a change notification, otherwise activeGrouping is oblivious to the initial value.
        // The simpler approach is to use the property setter, but then the compiler complains that
        // _selectedGroupingOption is null when existing constructor.
        this.RaisePropertyChanged(nameof(SelectedGroupingOption));
        
        foreach (GroupingOption groupingOption in GroupingOptions)
        {
            groupingOption.Selected
                .Subscribe(option => SelectedGroupingOption = option); // TODO: dispose
        }
        
        // This depends on _selectedGroupingOption being set
        RebuildCurrentlyDisplayedMods();

        Observable
            .Merge(
                activeGrouping.Select(_ => Unit.Default),
                Mods.Connect().Select(_ => Unit.Default),
                ModsGridCollectionView.SortDescriptions.ObserveCollectionChanges().Select(_ => Unit.Default)
            )
            .Subscribe(_ => RebuildCurrentlyDisplayedMods()); // TODO: dispose
    }

    // ObservableCollection doesn't support batch adding (outside of instantiation)
    // and I would prefer to not sort the list 1k times when the app initially starts
    // for *some people*
    public SourceList<ModEntryViewModel> Mods { get; } = new();

    public DataGridCollectionView ModsGridCollectionView { get; }

    public GroupingOption SelectedGroupingOption
    {
        get => _selectedGroupingOption;
        set => this.RaiseAndSetIfChanged(ref _selectedGroupingOption, value);
    }
}

public class DesignTimeModListViewModel : ModListViewModel
{
    public DesignTimeModListViewModel()
    {
        PopulateDummy(this);
    }

    public static void PopulateDummy(ModListViewModel viewModel)
    {
        viewModel.Mods.AddRange(new []
        {
            new ModEntryViewModel
            {
                IsEnabled = true,
                Title = "The most unique voicepack on workshop",
                Category = "Voicepacks",
                Author = "Author 3",
            },
            new ModEntryViewModel
            {
                IsEnabled = false,
                Title = "LWTOC",
                Category = "Gameplay",
                Author = "Author 1",
            },
            new ModEntryViewModel
            {
                IsEnabled = true,
                Title = "CI",
                Category = "Gameplay",
                Author = "Author 2",
            },
            new ModEntryViewModel
            {
                IsEnabled = true,
                Title = "A boring voicepack",
                Category = "Voicepacks",
                Author = "Author 3",
            },
            new ModEntryViewModel
            {
                IsEnabled = true,
                Title = "A boring voicepack2",
                Category = "Voicepacks",
                Author = "Author 3",
            }
        });
    }
}