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
    }

    private void Window_OnClosing(object? sender, CancelEventArgs e)
    {
        //
    }
}