using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModernX2Launcher.Views;

public partial class ModListView : UserControl
{
    public ModListView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}