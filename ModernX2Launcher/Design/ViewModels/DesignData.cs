using ModernX2Launcher.ViewModels.MainWindow.ModList;

namespace ModernX2Launcher.Design.ViewModels;

public static class DesignData
{
    public static ModListViewModel ModList
    {
        get
        {
            ModListViewModel vm = new();
            
            DummyMods.Populate(vm);

            return vm;
        }
    }
}