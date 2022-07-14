using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DynamicData;
using ModernX2Launcher.Utilities;
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
        MenuItem CreateSetCategoryItem(ModListViewModel.SetSelectedModsCategoryOption option)
        {
            MenuItem menuItem = new() { Header = option.Label };

            // This is as stupid as it looks. However, since MenuItem doesn't listen to CanExecute
            // changes while not attached to the logical tree (and the selection changes happen before
            // the menu is opened), our updates are lost. Additionally, both triggering the CanExecuteChanged
            // event and the MenuItem::CanExecuteChanged handler are not public, so the only way to force it
            // to run is via command (and command parameter) change handlers.
            // This will be fixed in avalonia v0.11
            menuItem.AttachedToLogicalTree += (_, _) => menuItem.Command = option.Command;
            menuItem.DetachedFromLogicalTree += (_, _) => menuItem.Command = null;

            return menuItem;
        }

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

                    [!ItemsControl.ItemsProperty] = ViewModel!.SetSelectedModsCategoryOptionStream
                        .Transform(CreateSetCategoryItem)
                        .Snapshots()
                        .ToBinding()
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