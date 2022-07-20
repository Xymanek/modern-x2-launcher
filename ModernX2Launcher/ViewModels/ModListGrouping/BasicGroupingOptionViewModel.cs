using System;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels.ModListGrouping;

/// <summary>
/// A basic option that is either selected or not
/// </summary>
public class BasicGroupingOptionViewModel : GroupingOptionBase
{
    private readonly IGroupingStrategy _strategy;

    public BasicGroupingOptionViewModel(IGroupingStrategy strategy)
    {
        _strategy = strategy;

        ButtonClicked = ReactiveCommand.Create(
            () => SelectedByUserSubject.OnNext(Unit.Default),
            this.WhenAnyValue(vm => vm.IsActive).Select(isActive => !isActive)
        );
    }

    public override IObservable<IGroupingStrategy> GroupingStrategy => Observable.Return(_strategy);

    public ReactiveCommand<Unit, Unit> ButtonClicked { get; }
}

public class DesignTimeBasicGroupingOptionViewModel : BasicGroupingOptionViewModel
{
    public DesignTimeBasicGroupingOptionViewModel() : base(null!)
    {
    }
}
