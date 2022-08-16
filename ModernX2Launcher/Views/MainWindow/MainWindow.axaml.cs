using Avalonia.ReactiveUI;
using ModernX2Launcher.ViewModels.MainWindow;

namespace ModernX2Launcher.Views.MainWindow;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}