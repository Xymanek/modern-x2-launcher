using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material.Icons;

namespace ModernX2Launcher.Views.Controls;

public class IconStatBlock : ContentControl
{
    public static readonly StyledProperty<MaterialIconKind> IconProperty =
        AvaloniaProperty.Register<IconStatBlock, MaterialIconKind>(nameof(Icon));
    
    public static readonly StyledProperty<IBrush?> IconForegroundProperty =
            AvaloniaProperty.Register<IconStatBlock, IBrush?>(nameof(Foreground), Brushes.Black);
    
    public MaterialIconKind Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
    
    public IBrush? IconForeground
    {
        get => GetValue(IconForegroundProperty);
        set => SetValue(IconForegroundProperty, value);
    }
}