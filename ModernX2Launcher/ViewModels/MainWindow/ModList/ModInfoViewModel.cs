using ModernX2Launcher.ViewModels.Common;
using ReactiveUI;
using Xymanek.ReactiveUI.Toolkit;

namespace ModernX2Launcher.ViewModels.MainWindow.ModList;

public partial class ModInfoViewModel : ViewModelBase
{
    private ModEntryViewModel? _modEntry;
    private ConfigFileEditorViewModel? _activeConfigEditor = new();

    /// <summary>
    /// Changes the hint text
    /// </summary>
    [ReactiveProperty]
    private bool _multipleSelected;

    public ModEntryViewModel? ModEntry
    {
        get => _modEntry;
        set => this.RaiseAndSetIfChanged(ref _modEntry, value);
    }

    public ConfigFileEditorViewModel? ActiveConfigEditor
    {
        get => _activeConfigEditor;
        set => this.RaiseAndSetIfChanged(ref _activeConfigEditor, value);
    }
}