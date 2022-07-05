using System.Collections.ObjectModel;

namespace ModernX2Launcher.ViewModels;

public class ModListViewModel : ViewModelBase
{
    public ModListViewModel()
    {
        Mods.Add(new ModViewModel
        {
            IsEnabled = false,
            Title = "LWTOC",
        });

        Mods.Add(new ModViewModel
        {
            IsEnabled = true,
            Title = "CI",
        });
    }

    public ObservableCollection<ModViewModel> Mods { get; } = new();
}