using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModernX2Launcher.Views;

public partial class ManageModListFilterDialog : Window
{
    public ManageModListFilterDialog()
    {
        InitializeComponent();
        
        AuthorSelectionListBox.Items = new object[] { "123", "456", "789" };
    }

    private void Window_OnClosing(object? sender, CancelEventArgs e)
    {
        //
    }
}