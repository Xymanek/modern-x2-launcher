using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using DynamicData;
using ModernX2Launcher.Utilities;
using ModernX2Launcher.ViewModels;
using ReactiveUI;

namespace ModernX2Launcher.Views;

public partial class ModListView : ReactiveUserControl<ModListViewModel>
{
    public ModListView()
    {
        InitializeComponent();

        this.WhenActivated(d => d(ViewModel!.ShowFilterDialog.RegisterHandler(ShowEditFilterDialogAsync)));
    }

    private void DataGrid_OnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.ContextMenu = new ContextMenu
        {
            Items = new object[]
            {
                new MenuItem { Header = "Enable" }.SetCommandFixedCanExecute(ViewModel!.EnableSelectedMods),
                new MenuItem { Header = "Disable" }.SetCommandFixedCanExecute(ViewModel!.DisableSelectedMods),
                
                new Separator(),

                new MenuItem
                {
                    Header = "Move to category",

                    [!ItemsControl.ItemsProperty] = ViewModel!.SetSelectedModsCategoryOptionStream
                        .Transform(
                            option => new MenuItem { Header = option.Label }.SetCommandFixedCanExecute(option.Command)
                        )
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

    private void OnToggleModEnabled(object? sender, RoutedEventArgs e)
    {
        CheckBox checkBox = (CheckBox)sender!;
        ModEntryViewModel interactedMod = (ModEntryViewModel)checkBox.DataContext!;

        ViewModel!.ToggleModEnabled.Execute(interactedMod)
            .Subscribe();
    }
    
    private async Task ShowEditFilterDialogAsync(InteractionContext<Unit, Unit> interaction)
    {
        EditModListFilterWindow dialog = new();
        // dialog.DataContext = interaction.Input;

        /*var result =*/ await dialog.ShowDialog(this.FindAncestorOfType<Window>());
        interaction.SetOutput(Unit.Default);
    }
}