using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Material.Icons;

namespace ModernX2Launcher.ViewModels.MainWindowModes;

public class ProfileModeViewModel : ViewModelBase, IMainWindowMode
{
    public string ModeName => "Profile management";
    public MaterialIconKind ModeIcon => MaterialIconKind.TextBoxMultipleOutline;

    public IObservable<IReadOnlyList<IMenuItemViewModel>?> AdditionalMenuItems
        => Observable.Return(Array.Empty<IMenuItemViewModel>());
}