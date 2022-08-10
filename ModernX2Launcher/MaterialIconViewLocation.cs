using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Material.Icons;
using ModernX2Launcher.Views;

namespace ModernX2Launcher;

public class MaterialIconViewLocation : IDataTemplate
{
    public IControl Build(object param)
    {
        return new MaterialIconView
        {
            DataContext = param
        };
    }

    public bool Match(object data)
    {
        return data is MaterialIconKind;
    }
}