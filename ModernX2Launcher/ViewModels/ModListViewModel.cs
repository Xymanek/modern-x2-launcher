using System.Collections.ObjectModel;
using Avalonia.Collections;

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
        
        ModsGridCollectionView = new DataGridCollectionView(Mods);
        
        ModsGridCollectionView.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(ModViewModel.Category)));
    }

    public ObservableCollection<ModViewModel> Mods { get; } = new();

    public DataGridCollectionView ModsGridCollectionView { get; }
}