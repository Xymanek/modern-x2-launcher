using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Collections;
using DynamicData;
using DynamicData.Aggregation;
using Material.Icons;
using ModernX2Launcher.Utilities;
using ModernX2Launcher.ViewModels.Common;
using ModernX2Launcher.ViewModels.MainWindow.ModList.Filtering;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels.MainWindow.ModList;

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

            MenuItem = new MenuItemViewModel
            {
                Header = label,
                
                // No point in disabling the menu item, so a separate command 
                Command = ReactiveCommand.Create(() => Selected.Execute().Subscribe()),
            };
        }

        public string Label { get; }
        public IGroupingStrategy Strategy { get; }

        public ReactiveCommand<Unit, Unit> Selected { get; }

        public IObservable<bool> IsActive { get; }
        
        public MenuItemViewModel MenuItem { get; }
    }

    public IReadOnlyList<GroupingOption> GroupingOptions { get; }

    // Should only ever be manipulated from SetupDisplayedMods.SetListContents
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

        GroupingMenuItem = new MenuItemViewModel
        {
            Header = "Grouping",
            
            // TODO: this is hax, but the entire grouping code needs to be redone, especially since
            // the switch from UI buttons to menu items
            Items = new IMenuItemViewModel[]
            {
                GroupingOptions[0].MenuItem,
                
                new MenuItemSeparatorViewModel(),
                
                GroupingOptions[1].MenuItem,

                new MenuItemSeparatorViewModel(),
                
                GroupingOptions[2].MenuItem,
                GroupingOptions[3].MenuItem,
            }
        };
        
        this.WhenActivated(disposable =>
        {
            foreach (GroupingOption groupingOption in GroupingOptions)
            {
                groupingOption.Selected
                    .Subscribe(_ => SelectedGroupingOption = groupingOption)
                    .DisposeWith(disposable);
                
                groupingOption.IsActive
                    .Subscribe(isActive => groupingOption.MenuItem.Icon = isActive ? MaterialIconKind.Tick : null)
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

        _enabledCount = Mods.Connect()
            .FilterOnObservable(mod => mod.WhenAnyValue(m => m.IsEnabled))
            .Count()
            .ToProperty(this, nameof(EnabledCount));

        AddFilter = ReactiveCommand.CreateFromTask(AddFilterImpl);
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
            .FilterOnObservable(DoesModPassFilters)
            .AutoRefreshOnObservable(WhenResortMod) // Note: SuppressRefresh after Sort breaks *all* sorting 
            .SortFixed(listComparerObs)
            .Snapshots()
            .CombineLatest(groupDescriptionObs)
            
            // Changing grouping will emit multiple times (at least 1 for resort and 1 for group change)
            // but we only want the final combination - there is no point in rebuilding the grid with
            // the intermediary values.
            // Likewise, when initially discovering mods there is no point in refreshing the grid multiple times per
            // tick with the newly added mods.
            .ThrottlePerTick()
            .Subscribe(tuple => SetListContents(tuple.First, tuple.Second));

        void SetListContents(IEnumerable<ModEntryViewModel> mods, DataGridGroupDescription? groupDescription)
        {
            using (ModsGridCollectionView.DeferRefresh())
            {
                this.RaisePropertyChanging(nameof(PostFilterCount));
                
                _currentDisplayedMods.Clear();
                _currentDisplayedMods.AddRange(mods);

                this.RaisePropertyChanged(nameof(PostFilterCount));
                
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

    private readonly ObservableAsPropertyHelper<int> _enabledCount;
    public int EnabledCount => _enabledCount.Value;

    public int PostFilterCount => _currentDisplayedMods.Count;
    
    public IMenuItemViewModel GroupingMenuItem { get; }
}