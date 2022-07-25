using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Collections;
using DynamicData;
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

            SetupDisplayedMods().DisposeWith(disposable);
        });

        // In future, this will come from the configured options (rather than derived from mods)
        SetSelectedModsCategoryOptionStream = Mods.Connect()
            .DistinctValues(modVm => modVm.Category)
            .Transform(category => new SetSelectedModsCategoryOption(category, this));

        ToggleModEnabled = ReactiveCommand.Create<ModEntryViewModel>(OnToggleModEnabled);

        EnableSelectedMods = ReactiveCommand.Create(
            OnEnableSelectedMods,
            WhenAnySelectedModsAreNot(mod => mod.IsEnabled)
        );
        
        DisableSelectedMods = ReactiveCommand.Create(
            OnDisableSelectedMods,
            WhenAnySelectedModsAre(mod => mod.IsEnabled)
        );
    }

    private GroupingOption SetupGroupingOption(string label, IGroupingStrategy strategy)
    {
        return new GroupingOption(label, strategy, this.WhenAnyValue(m => m.SelectedGroupingOption));
    }

    private IDisposable SetupDisplayedMods()
    {
        IObservable<IGroupingStrategy> groupingStrategyObs = this.WhenAnyValue(vm => vm.SelectedGroupingOption)
            .Select(option => option.Strategy);

        IObservable<IReadOnlyCollection<ModEntrySorter>> sortersObs = groupingStrategyObs
            .Select(strategy =>
            {
                return ModsGridCollectionView.SortDescriptions
                    .ToObservableChangeSet()
                    .QueryWhenChanged()

                    // When changing sorting, first the list is cleared and then the new sort description is added.
                    // We always want to sort by something, so we skip the "descriptions cleared" emit.
                    // Not using Snapshots() above for the same reason.
                    .Where(sortDescriptions => sortDescriptions.Any())
                    .FilterEach(sortDescription =>
                    {
                        return strategy.ShouldSkipSortDescription(sortDescription)
                            .Select(b => !b);
                    })
                    .Transform(CreateSorterFromSortDescription)
                    .CombineLatest(strategy.PrimarySorterObs)
                    .Select(tuple =>
                    {
                        (IReadOnlyCollection<ModEntrySorter> sorters, ModEntrySorter? primarySorter) = tuple;

                        if (primarySorter != null)
                        {
                            sorters = sorters.Prepend(primarySorter).ToArray();
                        }

                        return sorters;
                    });
            })
            .Switch();

        IObservable<IComparer<ModEntryViewModel>> listComparerObs = sortersObs
            .Select(sorters => sorters.Select(sorter => sorter.AsComparer()).ToStack());

        IObservable<Unit> WhenResortMod(ModEntryViewModel mod)
        {
            return sortersObs
                .Select(sorters =>
                {
                    return sorters
                        .Select(sorter => sorter.ResortObservableProvider)
                        .WhereNotNull()
                        .Select(resortProvider => resortProvider(mod))
                        .Merge();
                })
                .Switch();
        }

        IObservable<DataGridGroupDescription?> groupDescriptionObs = groupingStrategyObs
            .Select(strategy => strategy.GroupDescriptionObs)
            .Switch();

        return Mods.Connect()
            .AutoRefreshOnObservable(WhenResortMod) // Note: SuppressRefresh after Sort breaks *all* sorting 
            .SortFixed(listComparerObs)
            .Snapshots()
            // TODO: How to not fire twice on grouping change? (comparer + description changes)
            .CombineLatest(groupDescriptionObs)
            .Subscribe(tuple => SetListContents(tuple.First, tuple.Second));

        void SetListContents(IEnumerable<ModEntryViewModel> mods, DataGridGroupDescription? groupDescription)
        {
            using (ModsGridCollectionView.DeferRefresh())
            {
                _currentDisplayedMods.Clear();
                _currentDisplayedMods.AddRange(mods);

                ModsGridCollectionView.GroupDescriptions.Clear();
                if (groupDescription != null) ModsGridCollectionView.GroupDescriptions.Add(groupDescription);

                // Force the reset of tracking enumerator and refresh of the data grid
                // (see Avalonia.Collections.DataGridCollectionView.EnsureCollectionInSync).
                //
                // Calling ModsGridCollectionView.Refresh() here would be suboptimal as
                // that would not reset the tracking enumerator, hence likely causing
                // a second (unnecessary) refresh of the collection view later.
                _ = ModsGridCollectionView.IsEmpty;
            }
        }
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