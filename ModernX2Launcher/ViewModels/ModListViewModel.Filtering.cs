using System;
using System.Reactive;
using System.Reactive.Linq;
using ModernX2Launcher.Utilities;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

public partial class ModListViewModel
{
    private static readonly TimeSpan NameFilterThrottle = TimeSpan.FromSeconds(0.2);
    
    private string _nameFilterValue = "";

    public string NameFilterValue
    {
        get => _nameFilterValue;
        set => this.RaiseAndSetIfChanged(ref _nameFilterValue, value);
    }

    private IObservable<bool> DoesModPassFilters(ModEntryViewModel mod)
    {
        return this.WhenAnyValue(m => m.NameFilterValue)
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
    }
}