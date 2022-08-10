using System;
using System.Collections.Generic;

namespace ModernX2Launcher.ViewModels.MainWindowModes;

public interface IMainWindowMode
{
    IObservable<IReadOnlyList<IMenuItemViewModel>?> AdditionalMenuItems { get; }
}