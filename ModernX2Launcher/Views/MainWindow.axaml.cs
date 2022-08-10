using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.ReactiveUI;
using ModernX2Launcher.ViewModels;

namespace ModernX2Launcher.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public static readonly FuncTemplate<IPanel> ModesPanel = new(
        () => new StackPanel { Orientation = Orientation.Vertical}
    );
    
    public MainWindow()
    {
        InitializeComponent();
    }
}