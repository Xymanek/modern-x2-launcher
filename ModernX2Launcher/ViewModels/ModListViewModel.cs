using System.Collections.ObjectModel;
using Avalonia.Collections;

namespace ModernX2Launcher.ViewModels;

public class ModListViewModel : ViewModelBase
{
    public ModListViewModel()
    {
        Mods.Add(new ModEntryViewModel
        {
            IsEnabled = false,
            Title = "LWTOC",
            Category = "Gameplay",
            Author = "Author 1",
        });

        Mods.Add(new ModEntryViewModel
        {
            IsEnabled = true,
            Title = "CI",
            Category = "Gameplay",
            Author = "Author 2",
        });

        Mods.Add(new ModEntryViewModel
        {
            IsEnabled = true,
            Title = "The most unique voicepack on workshop",
            Category = "Voicepacks",
            Author = "Author 3",
        });
        
        Mods.Add(new ModEntryViewModel
        {
            IsEnabled = true,
            Title = "A boring voicepack",
            Category = "Voicepacks",
            Author = "Author 3",
        });

        ModsGridCollectionView = new DataGridCollectionView(Mods);

        ModsGridCollectionView.GroupDescriptions.Add(
            new DataGridPathGroupDescription(nameof(ModEntryViewModel.Category))
        );
    }

    public ObservableCollection<ModEntryViewModel> Mods { get; } = new();

    public DataGridCollectionView ModsGridCollectionView { get; }
}