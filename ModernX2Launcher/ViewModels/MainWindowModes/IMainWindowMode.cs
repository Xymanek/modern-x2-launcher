using System;
using System.Collections.Generic;
using Material.Icons;

namespace ModernX2Launcher.ViewModels.MainWindowModes;

public interface IMainWindowMode
{
    string ModeName { get; }
    
    MaterialIconKind ModeIcon { get; }
    
    IObservable<IReadOnlyList<IMenuItemViewModel>> AdditionalMenuItems { get; }
}