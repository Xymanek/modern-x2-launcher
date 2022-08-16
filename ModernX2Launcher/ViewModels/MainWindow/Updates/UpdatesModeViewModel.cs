using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Material.Icons;
using ModernX2Launcher.ViewModels.Common;

namespace ModernX2Launcher.ViewModels.MainWindow.Updates;

public class UpdatesModeViewModel : ViewModelBase, IMainWindowMode
{
    public string ModeName => "Mod updates";
    public MaterialIconKind ModeIcon => MaterialIconKind.Update;

    public IObservable<IReadOnlyList<IMenuItemViewModel>> AdditionalMenuItems
        => Observable.Empty<IReadOnlyList<IMenuItemViewModel>>();
}