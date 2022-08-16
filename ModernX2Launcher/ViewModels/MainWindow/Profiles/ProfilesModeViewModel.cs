using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Material.Icons;
using ModernX2Launcher.ViewModels.Common;

namespace ModernX2Launcher.ViewModels.MainWindow.Profiles;

public class ProfilesModeViewModel : ViewModelBase, IMainWindowMode
{
    public string ModeName => "Profile management";
    public MaterialIconKind ModeIcon => MaterialIconKind.TextBoxMultipleOutline;

    public IObservable<IReadOnlyList<IMenuItemViewModel>> AdditionalMenuItems
        => Observable.Empty<IReadOnlyList<IMenuItemViewModel>>();
}