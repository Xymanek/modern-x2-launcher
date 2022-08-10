using ModernX2Launcher.ViewModels.MainWindowModes;

namespace ModernX2Launcher.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ModListModeViewModel ModListMode { get; } = new();
}