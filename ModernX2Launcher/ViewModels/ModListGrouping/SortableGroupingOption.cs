using System;
using System.Reactive.Linq;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels.ModListGrouping;

public class SortableGroupingOption : GroupingOptionBase
{
    private bool _isDescending;

    public SortableGroupingOption(
        IObservable<GroupingOptionBase> activeGroupingOption,
        IGroupingStrategy ascendingStrategy, IGroupingStrategy descendingStrategy
    )
        : base(activeGroupingOption)
    {
        GroupingStrategy = this.WhenAnyValue(o => o.IsDescending)
            .Select(isDescending => isDescending ? descendingStrategy : ascendingStrategy);
    }

    public override IObservable<IGroupingStrategy> GroupingStrategy { get; }

    public bool IsDescending
    {
        get => _isDescending;
        set => this.RaiseAndSetIfChanged(ref _isDescending, value);
    }
}