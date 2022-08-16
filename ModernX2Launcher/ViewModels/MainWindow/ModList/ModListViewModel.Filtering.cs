using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using ModernX2Launcher.Utilities;
using ModernX2Launcher.ViewModels.Common;
using ModernX2Launcher.ViewModels.MainWindow.ModList.Filtering;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels.MainWindow.ModList;

public partial class ModListViewModel
{
    private static readonly TimeSpan NameFilterThrottle = TimeSpan.FromSeconds(0.2);

    private string _nameFilterValue = "";

    public string NameFilterValue
    {
        get => _nameFilterValue;
        set => this.RaiseAndSetIfChanged(ref _nameFilterValue, value);
    }

    public SourceList<IModListFilter> ActiveFilters { get; } = new();

    public ReactiveCommand<Unit, Unit> AddFilter { get; }

    public Interaction<Unit, Unit> ShowFilterDialog { get; } = new();

    private async Task AddFilterImpl()
    {
        // ActiveFilters.Add(new IsEnabledFilter(BoolModFilterKind.ExcludeFalse));

        await ShowFilterDialog.Handle(Unit.Default);
    }

    private IObservable<bool> DoesModPassFilters(ModEntryViewModel mod)
    {
        IObservable<bool> nameFilter = this.WhenAnyValue(m => m.NameFilterValue)
            .ThrottleExcludingFirst(nameFilterValue =>
            {
                IObservable<Unit> triggerObservable = Observable.Return(Unit.Default);

                // Normally we want to wait a bit so that we don't continuously refresh while the user is typing.
                // However, when the filter was set to empty (e.g. the clear button was clicked),
                // we want to instantly refresh the list (very unlikely that the user will keep typing).
                if (!string.IsNullOrEmpty(nameFilterValue))
                {
                    triggerObservable = triggerObservable.Delay(NameFilterThrottle);
                }

                return triggerObservable;
            })
            .DistinctUntilChanged()
            .Select(nameFilterValue =>
            {
                // No filter = all mods pass
                if (string.IsNullOrEmpty(nameFilterValue)) return Observable.Return(true);

                // Mods have a single name regardless of the user's locale so invariant.
                // Ignore case because it's easier to search without caring for capitalization.
                return mod.WhenAnyValue(m => m.Title)
                    .Select(title => title.Contains(nameFilterValue, StringComparison.InvariantCultureIgnoreCase));
            })
            .Switch();

        return ActiveFilters.Connect()
            .Snapshots()
            .Select(filters => filters
                .Select(filter => filter.DoesModPass(mod))
                .Append(nameFilter)
                .CombineLatest()
                .Select(filterResults => filterResults.All(b => b))
            )
            .Switch();
    }
}