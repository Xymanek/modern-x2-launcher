using System.Collections.Generic;
using System.Windows.Input;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

public interface IMenuItemViewModel
{
    string Header { get; }
    object? Icon { get; }
    
    ICommand? Command { get; }
    object? CommandParameter { get; }
    
    IReadOnlyList<IMenuItemViewModel>? Items { get; }
}

public class MenuItemViewModel : ReactiveObject, IMenuItemViewModel
{
    private string _header = "";
    private object? _icon;
    
    private ICommand? _command;
    private object? _commandParameter;
    
    private IReadOnlyList<IMenuItemViewModel>? _items;

    public string Header
    {
        get => _header;
        set => this.RaiseAndSetIfChanged(ref _header, value);
    }

    public object? Icon
    {
        get => _icon;
        set => this.RaiseAndSetIfChanged(ref _icon, value);
    }

    public ICommand? Command
    {
        get => _command;
        set => this.RaiseAndSetIfChanged(ref _command, value);
    }

    public object? CommandParameter
    {
        get => _commandParameter;
        set => this.RaiseAndSetIfChanged(ref _commandParameter, value);
    }

    public IReadOnlyList<IMenuItemViewModel>? Items
    {
        get => _items;
        set => this.RaiseAndSetIfChanged(ref _items, value);
    }
}

public class MenuItemSeparatorViewModel : IMenuItemViewModel
{
    public string Header => "-";

    public object? Icon => null;
    public ICommand? Command => null;
    public object? CommandParameter => null;
    public IReadOnlyList<IMenuItemViewModel>? Items => null;
}