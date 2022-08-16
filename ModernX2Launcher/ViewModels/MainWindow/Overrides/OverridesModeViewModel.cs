using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Material.Icons;
using ModernX2Launcher.ViewModels.Common;

namespace ModernX2Launcher.ViewModels.MainWindow.Overrides;

public class OverridesModeViewModel : ViewModelBase, IMainWindowMode
{
    public string ModeName => "Overrides";
    public MaterialIconKind ModeIcon => MaterialIconKind.AlertRhombusOutline;

    public IObservable<IReadOnlyList<IMenuItemViewModel>> AdditionalMenuItems
        => Observable.Empty<IReadOnlyList<IMenuItemViewModel>>();
}