using System;
using System.Reactive;
using System.Reactive.Subjects;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels.ModListGrouping;

public abstract class GroupingOptionBase : ViewModelBase
{
    /// <summary>
    /// Publish a value here whenever the user clicks/selects this option. 
    /// </summary>
    protected readonly Subject<Unit> SelectedByUserSubject = new();

    protected GroupingOptionBase()
    {
        SelectedByUser = SelectedByUserSubject;
    }

    /// <summary>
    /// Emits a value when the user selects this options. Will cause this option to become the active one. 
    /// </summary>
    public IObservable<Unit> SelectedByUser { get; }

    /// <summary>
    /// Observable that returns the grouping strategy to use. Will be subscribed to while this option is active.
    /// Must emit a value when subscribed to. 
    /// </summary>
    public abstract IObservable<IGroupingStrategy> GroupingStrategy { get; }

    private bool _isActive;
    private string _label = "";

    public bool IsActive
    {
        get => _isActive;
        set => this.RaiseAndSetIfChanged(ref _isActive, value);
    }

    public string Label
    {
        get => _label;
        set => this.RaiseAndSetIfChanged(ref _label, value);
    }
}