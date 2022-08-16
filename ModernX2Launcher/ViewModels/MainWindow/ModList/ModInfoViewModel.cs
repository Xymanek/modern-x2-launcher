using ModernX2Launcher.ViewModels.Common;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels.MainWindow.ModList;

public class ModInfoViewModel : ViewModelBase
{
    private ModEntryViewModel? _modEntry;
    private ConfigFileEditorViewModel? _activeConfigEditor = new();

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