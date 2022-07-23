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

            SetupDisplayedMods().DisposeWith(disposable);
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

    private IDisposable SetupDisplayedMods()
    {
        IObservable<IGroupingStrategy> groupingStrategyObs = this.WhenAnyValue(vm => vm.SelectedGroupingOption)
            .Select(option => option.Strategy);

        // IObservable<IReadOnlyCollection<ModEntrySorter>> sortersObs = groupingStrategyObs
        //     .SelectMany(strategy =>
        //     {
        //         IObservable<IReadOnlyCollection<ModEntrySorter>> sortersObs = ModsGridCollectionView.SortDescriptions
        //             .ToObservableChangeSet()
        //             .FilterOnObservable(
        //                 sortDescription => strategy
        //                     .ShouldSkipSortDescription(sortDescription)
        //                     .Select(b => !b)
        //             )
        //             .Transform(CreateSorterFromSortDescription)
        //             .QueryWhenChanged() // Not Snapshots because we don't want to sort by nothing
        //             .CombineLatest(strategy.GetPrimarySorter())
        //             .Select(tuple =>
        //             {
        //                 (IReadOnlyCollection<ModEntrySorter> sorters, ModEntrySorter? primarySorter) = tuple;
        //
        //                 if (primarySorter != null)
        //                 {
        //                     sorters = sorters.Prepend(primarySorter).ToArray();
        //                 }
        //
        //                 return sorters;
        //             });
        //
        //         return sortersObs;
        //     });
        //
        // IObservable<IComparer<ModEntryViewModel>> listComparerObs = sortersObs
        //     .Select(sorters => sorters.Select(sorter => sorter.AsComparer()).ToStack());
        //
        // IObservable<Unit> WhenResortMod(ModEntryViewModel mod)
        // {
        //     return sortersObs.SelectMany(sorters =>
        //     {
        //         return sorters.Select(sorter => sorter.ResortObservableProvider)
        //             .WhereNotNull()
        //             .Select(resortProvider => resortProvider(mod))
        //             .Merge();
        //     });
        // }
        //
        // // This is really stupid, but using .Sort(listComparerObs) causes comparisons of
        // // preexisting elements (when subscribing) to happen before the comparer is received
        // // so the default comparer is used, which throws as mod VM doesn't implement IComparable
        // return listComparerObs
        //     .SelectMany(
        //         comparer => Mods.Connect()
        //             .AutoRefreshOnObservable(WhenResortMod)
        //             .Sort(comparer, comparerChanged: listComparerObs.Skip(1))
        //             .SuppressRefresh()
        //     )
        //     .Snapshots()
        //     .CombineLatest(groupingStrategyObs.SelectMany(strategy => strategy.GetGroupDescription()))
        //     // TODO: How to not fire twice on grouping change? (comparer + description changes)
        //     // TODO: this is never reached
        //     .Subscribe(tuple => SetListContents(tuple.First, tuple.Second));

        IObservable<IReadOnlyCollection<ModEntrySorter>> sortersObs = ModsGridCollectionView.SortDescriptions
            .ToObservableChangeSet()
            .Transform(CreateSorterFromSortDescription)
            .QueryWhenChanged()

            // When changing sorting, first the list is cleared and then the new sort description is added.
            // We always want to sort by something, so we skip the "descriptions cleared" emit.
            // Not using Snapshots above for the same reason.
            .Where(sorters => sorters.Any());

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
            .Select(strategy => strategy.GetGroupDescription())
            .Switch();

        return Mods.Connect()
            .AutoRefreshOnObservable(WhenResortMod) // Note: SuppressRefresh after Sort breaks *all* sorting 
            .SortFixed(listComparerObs)
            .Snapshots()
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