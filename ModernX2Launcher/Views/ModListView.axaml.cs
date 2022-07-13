using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
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

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ViewModel?.SelectedMods.Edit(mods =>
        {
            mods.AddRange(e.AddedItems.OfType<ModEntryViewModel>());
            mods.RemoveMany(e.RemovedItems.OfType<ModEntryViewModel>());
        });
    }

    private void DataGridContextMenu_OnOpening(object? sender, CancelEventArgs e)
    {
        if (!ViewModel?.IsDataGridContextMenuEnabled ?? false)
        {
            e.Cancel = true;
        }
    }
}