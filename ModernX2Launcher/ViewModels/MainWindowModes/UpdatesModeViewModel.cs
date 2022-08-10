using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Material.Icons;

namespace ModernX2Launcher.ViewModels.MainWindowModes;

public class UpdatesModeViewModel : ViewModelBase, IMainWindowMode
{
    public string ModeName => "Mod updates";
    public MaterialIconKind ModeIcon => MaterialIconKind.Update;

    public IObservable<IReadOnlyList<IMenuItemViewModel>> AdditionalMenuItems
        => Observable.Empty<IReadOnlyList<IMenuItemViewModel>>();
}