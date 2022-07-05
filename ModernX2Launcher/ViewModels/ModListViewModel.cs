using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Data.Converters;

namespace ModernX2Launcher.ViewModels;

public class ModListViewModel : ViewModelBase
{
    private static readonly DataGridGroupDescription GroupingByCategory =
        new DataGridPathGroupDescription(nameof(ModEntryViewModel.Category));

    private static readonly IReadOnlyDictionary<string, DataGridGroupDescription> GroupingDescriptionsByProperty =
        new Dictionary<string, DataGridGroupDescription>
        {
            [nameof(ModEntryViewModel.Title)] = GroupingByCategory,
            [nameof(ModEntryViewModel.Category)] = GroupingByCategory,
            
            [nameof(ModEntryViewModel.Author)] = new DataGridPathGroupDescription(nameof(ModEntryViewModel.Author)),
            
            [nameof(ModEntryViewModel.IsEnabled)] =
                new DataGridPathGroupDescription(nameof(ModEntryViewModel.IsEnabled))
                {
                    ValueConverter = new FuncValueConverter<bool, string>(b => b ? "Enabled" : "Disabled")
                },
        };

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
            GroupingDescriptionsByProperty[nameof(ModEntryViewModel.IsEnabled)]
        );
    }

    public ObservableCollection<ModEntryViewModel> Mods { get; } = new();

    public DataGridCollectionView ModsGridCollectionView { get; }
}