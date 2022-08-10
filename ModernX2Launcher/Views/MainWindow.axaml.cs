using Avalonia.ReactiveUI;
using ModernX2Launcher.ViewModels;

namespace ModernX2Launcher.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}