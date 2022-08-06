using DynamicData;
using ModernX2Launcher.ViewModels;

namespace ModernX2Launcher;

public static class DummyMods
{
    public static void Populate(ModListViewModel viewModel)
    {
        viewModel.Mods.AddRange(new[]
        {
            new ModEntryViewModel
            {
                IsEnabled = true,
                Title = "The most unique voicepack on workshop",
                Category = "Voicepacks",
                Author = "Author 3",
            },
            new ModEntryViewModel
            {
                IsEnabled = false,
                Title = "LWTOC",
                Category = "Gameplay",
                Author = "Author 1",
            },
            new ModEntryViewModel
            {
                IsEnabled = true,
                Title = "CI",
                Category = "Gameplay",
                Author = "Author 2",
            },
            new ModEntryViewModel
            {
                IsEnabled = true,
                Title = "A boring voicepack",
                Category = "Voicepacks",
                Author = "Author 3",
            },
            new ModEntryViewModel
            {
                IsEnabled = true,
                Title = "A boring voicepack2",
                Category = "Voicepacks",
                Author = "Author 3",
            }
        });
    }
}