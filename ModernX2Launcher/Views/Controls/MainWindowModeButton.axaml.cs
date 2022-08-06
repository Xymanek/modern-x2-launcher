using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Material.Icons;

namespace ModernX2Launcher.Views.Controls;

public class MainWindowModeButton : TemplatedControl
{
    public static readonly StyledProperty<string> ModeNameProperty =
        AvaloniaProperty.Register<MainWindowModeButton, string>(nameof(ModeName));
    
    public static readonly StyledProperty<MaterialIconKind> IconProperty =
        AvaloniaProperty.Register<MainWindowModeButton, MaterialIconKind>(nameof(Icon));
    
    public string ModeName
    {
        get => GetValue(ModeNameProperty);
        set => SetValue(ModeNameProperty, value);
    }
    
    public MaterialIconKind Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}