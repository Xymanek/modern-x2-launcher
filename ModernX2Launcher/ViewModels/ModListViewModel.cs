using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Collections;
using DynamicData;
using DynamicData.Binding;
using ModernX2Launcher.Utilities;
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
                () => { },
                IsActive.Select(isActive => !isActive)
            );
        }

        public string Label { get; }
        public IGroupingStrategy Strategy { get; }

        public ReactiveCommand<Unit, Unit> Selected { get; }

        public IObservable<bool> IsActive { get; }
    }

    public sealed class SetSelectedModsCategoryOption
    {
        public SetSelectedModsCategoryOption(string category, ModListViewModel listViewModel)
        {
            Label = category;

            Command = ReactiveCommand.Create(
                (Unit _) =>
                {
                    foreach (ModEntryViewModel mod in listViewModel.SelectedMods.Items)
                    {
                        mod.Category = category;
                    }
                },
                listViewModel.SelectedMods.Connect()
                    .Snapshots()
                    .SelectMany(
                        selectedMods => selectedMods
                            .Select(mod => mod.WhenAnyValue(m => m.Category))
                            .CombineLatest()
                            .Select(selectedModsCategories => selectedModsCategories.Any(c => c != category))
                    )
            );
        }

        public string Label { get; }

        public ReactiveCommand<Unit, Unit> Command { get; }
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

        GroupingOptions = new[]
        {
            SetupGroupingOption("Disabled", new DisabledGroupingStrategy()),
            SetupGroupingOption("Based on sort", new SortBasedGroupingStrategy(ModsGridCollectionView)),

            SetupGroupingOption("Category", new FixedPropertyGroupingStrategy(nameof(ModEntryViewModel.Category))),
            SetupGroupingOption("Author", new FixedPropertyGroupingStrategy(nameof(ModEntryViewModel.Author))),
        };

        _selectedGroupingOption = GroupingOptions[1];

        // We need to raise a change notification, otherwise WhenAnyValue is oblivious to the initial value.
        // The simpler approach is to use the property setter, but then the compiler complains that
        // _selectedGroupingOption is null when existing constructor.
        this.RaisePropertyChanged(nameof(SelectedGroupingOption));

        this.WhenActivated(disposable =>
        {
            foreach (GroupingOption groupingOption in GroupingOptions)
            {
                groupingOption.Selected
                    .Subscribe(_ => SelectedGroupingOption = groupingOption)
                    .DisposeWith(disposable);
            }

            // Rebuild the displayed mods when
            Observable
                .Merge(
                    // The list of mods changes (this will also cause the initial rebuild)
                    Mods.Connect().Select(_ => Unit.Default),

                    // The active grouping mode is changed 
                    this.WhenAnyValue(m => m.SelectedGroupingOption).Select(_ => Unit.Default),

                    // The user clicks on a column header to change the sorting
                    ModsGridCollectionView.SortDescriptions.ObserveCollectionChanges().Select(_ => Unit.Default)
                )
                .SelectMany(_ =>
                {
                    Func<ModEntryViewModel, IObservable<Unit>>[] resortProviders = GetActiveSorters()
                        .Select(sorter => sorter.ResortObservableProvider)
                        .WhereNotNull()
                        .ToArray();

                    // Since we got here, we want the mod list to be rebuilt immediately
                    // without waiting for some mod entry property to change
                    ReplaySubject<Unit> replaySubject = new(1);
                    replaySubject.OnNext(Unit.Default);

                    return Mods.Items
                        .SelectMany(
                            _ => resortProviders,
                            (model, resortProvider) => resortProvider(model)
                        )
                        .Prepend(replaySubject);
                })
                .Merge()
                .Subscribe(_ => RebuildCurrentlyDisplayedMods())
                .DisposeWith(disposable);
        });

        // In future, this will come from the configured options (rather than derived from mods)
        SetSelectedModsCategoryOptionStream = Mods.Connect()
            .DistinctValues(modVm => modVm.Category)
            .Transform(category => new SetSelectedModsCategoryOption(category, this));
    }

    private GroupingOption SetupGroupingOption(string label, IGroupingStrategy strategy)
    {
        return new GroupingOption(label, strategy, this.WhenAnyValue(m => m.SelectedGroupingOption));
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

    public SourceList<ModEntryViewModel> SelectedMods { get; } = new();

    public IObservable<IChangeSet<SetSelectedModsCategoryOption>> SetSelectedModsCategoryOptionStream { get; }
}

public class DesignTimeModListViewModel : ModListViewModel
{
    public DesignTimeModListViewModel()
    {
        PopulateDummy(this);
    }

    public static void PopulateDummy(ModListViewModel viewModel)
    {
        viewModel.Mods.AddRange(new[]
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