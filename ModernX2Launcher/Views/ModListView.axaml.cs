using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DynamicData;
using ModernX2Launcher.ViewModels;

namespace ModernX2Launcher.Views;

public partial class ModListView : ReactiveUserControl<ModListViewModel>
{
    public ModListView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void DataGrid_OnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        ICommand command = ViewModel!.SetSelectedModsCategory;

        // Ideally the menu should come from xaml, but everything I've tried fails to bind
        // and I've spent too much time on this already
        e.Row.ContextMenu = new ContextMenu
        {
            Items = new object[]
            {
                new MenuItem { Header = "Test 1", IsEnabled = false },
                new MenuItem { Header = "Test 2" },

                new Separator(),

                new MenuItem
                {
                    Header = "Move to category",
                    Items = new object[]
                    {
                        new MenuItem { Header = "Gameplay", CommandParameter = "Gameplay", Command = command },
                        new MenuItem { Header = "Voicepacks", CommandParameter = "Voicepacks", Command = command },
                    },
                },
            },
        };
    }

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ViewModel?.SelectedMods.Edit(mods =>
        {
            mods.AddRange(e.AddedItems.OfType<ModEntryViewModel>());
            mods.RemoveMany(e.RemovedItems.OfType<ModEntryViewModel>());
        });
    }
}