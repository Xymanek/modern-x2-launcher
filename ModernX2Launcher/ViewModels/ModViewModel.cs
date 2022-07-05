﻿using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

/// <remarks>
/// Never will have an associated view, so not a <see cref="ViewModelBase"/>
/// </remarks>
public class ModViewModel : ReactiveObject
{
    private bool _isEnabled;
    private string _title = "";
    private string _category = "";

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string Category
    {
        get => _category;
        set => this.RaiseAndSetIfChanged(ref _category, value);
    }
}