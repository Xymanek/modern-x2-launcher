using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModernX2Launcher.Views.ModListGrouping;

public partial class BasicGroupingOptionView : UserControl
{
    public BasicGroupingOptionView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}