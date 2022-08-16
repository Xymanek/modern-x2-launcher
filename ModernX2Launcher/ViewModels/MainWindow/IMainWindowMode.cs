using System;
using System.Collections.Generic;
using Material.Icons;
using ModernX2Launcher.ViewModels.Common;

namespace ModernX2Launcher.ViewModels.MainWindow;

public interface IMainWindowMode
{
    string ModeName { get; }
    
    MaterialIconKind ModeIcon { get; }
    
    IObservable<IReadOnlyList<IMenuItemViewModel>> AdditionalMenuItems { get; }
}