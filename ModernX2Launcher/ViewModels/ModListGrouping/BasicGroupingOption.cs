using System;
using System.Reactive.Linq;

namespace ModernX2Launcher.ViewModels.ModListGrouping;

/// <summary>
/// A basic option that is either selected or not
/// </summary>
public class BasicGroupingOption : GroupingOptionBase
{
    private readonly IGroupingStrategy _strategy;

    public BasicGroupingOption(IObservable<GroupingOptionBase> activeGroupingOption, IGroupingStrategy strategy)
        : base(activeGroupingOption)
    {
        _strategy = strategy;
    }

    public override IObservable<IGroupingStrategy> GroupingStrategy => Observable.Return(_strategy);
}