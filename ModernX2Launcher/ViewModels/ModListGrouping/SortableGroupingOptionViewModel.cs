using System;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels.ModListGrouping;

public class SortableGroupingOptionViewModel : GroupingOptionBase
{
    private bool _isDescending;

    public SortableGroupingOptionViewModel(IGroupingStrategy ascendingStrategy, IGroupingStrategy descendingStrategy)
    {
        GroupingStrategy = this.WhenAnyValue(o => o.IsDescending)
            .Select(isDescending => isDescending ? descendingStrategy : ascendingStrategy);

        // Always available
        ButtonClicked = ReactiveCommand.Create(OnClicked);
    }

    private void OnClicked()
    {
        SelectedByUserSubject.OnNext(Unit.Default);

        if (IsActive)
        {
            IsDescending = !IsDescending;
        }
    }

    public override IObservable<IGroupingStrategy> GroupingStrategy { get; }

    public bool IsDescending
    {
        get => _isDescending;
        set => this.RaiseAndSetIfChanged(ref _isDescending, value);
    }

    public ReactiveCommand<Unit, Unit> ButtonClicked { get; }
}

public class DesignTimeSortableGroupingOptionViewModel : SortableGroupingOptionViewModel
{
    public DesignTimeSortableGroupingOptionViewModel() : base(null!, null!)
    {
    }
}
