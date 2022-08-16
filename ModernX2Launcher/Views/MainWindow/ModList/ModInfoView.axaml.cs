using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModernX2Launcher.Views.MainWindow.ModList;

public partial class ModInfoView : UserControl
{
    public ModInfoView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}