using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

public class ModInfoViewModel : ViewModelBase
{
    private ModEntryViewModel? _modEntry;

    public ModEntryViewModel? ModEntry
    {
        get => _modEntry;
        set => this.RaiseAndSetIfChanged(ref _modEntry, value);
    }
}