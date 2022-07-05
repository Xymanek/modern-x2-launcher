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
            Category = "Gameplay",
        });

        Mods.Add(new ModViewModel
        {
            IsEnabled = true,
            Title = "CI",
            Category = "Gameplay",
        });
        
        Mods.Add(new ModViewModel
        {
            IsEnabled = true,
            Title = "The most unique voicepack on workshop",
            Category = "Voicepacks",
        });
    }

    public ObservableCollection<ModViewModel> Mods { get; } = new();
}