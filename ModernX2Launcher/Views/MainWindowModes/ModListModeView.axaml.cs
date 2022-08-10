using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ModernX2Launcher.ViewModels.MainWindowModes;

namespace ModernX2Launcher.Views.MainWindowModes;

public partial class ModListModeView : ReactiveUserControl<ModListModeViewModel>
{
    public ModListModeView()
    {
        InitializeComponent();
    }
}