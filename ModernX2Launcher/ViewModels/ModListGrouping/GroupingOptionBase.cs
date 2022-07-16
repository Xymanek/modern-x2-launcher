using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels.ModListGrouping;

public abstract class GroupingOptionBase : ViewModelBase
{
    protected readonly IObservable<GroupingOptionBase> ActiveGroupingOption;
    
    /// <summary>
    /// Publish a value here whenever the user clicks/selects this option. 
    /// </summary>
    protected readonly Subject<Unit> SelectedByUserSubject = new();

    protected GroupingOptionBase(IObservable<GroupingOptionBase> activeGroupingOption)
    {
        ActiveGroupingOption = activeGroupingOption;

        _isActive = ActiveGroupingOption
            .Select(activeOption => activeOption == this)
            .ToProperty(this, vm => vm.IsActive);

        SelectedByUser = SelectedByUserSubject
            .SkipWhile(_ => IsActive);
    }
    
    private readonly ObservableAsPropertyHelper<bool> _isActive;
    public bool IsActive => _isActive.Value;

    /// <summary>
    /// Emits a value when the user selects this options, except if this option is active already (e.g. for options
    /// with multiple states). Will cause this option to become the active one. 
    /// </summary>
    public IObservable<Unit> SelectedByUser { get; }

    /// <summary>
    /// Observable that returns the grouping strategy to use. Will be subscribed to while this option is active.
    /// Must emit a value when subscribed to. 
    /// </summary>
    public abstract IObservable<IGroupingStrategy> GroupingStrategy { get; }

    private string _label = "";

    public string Label
    {
        get => _label;
        set => this.RaiseAndSetIfChanged(ref _label, value);
    }
}