using System;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

public class ConfigFileEditorViewModel : ViewModelBase
{
    private string[] _lines = Array.Empty<string>();

    public string[] Lines
    {
        get => _lines;
        set => this.RaiseAndSetIfChanged(ref _lines, value);
    }
}