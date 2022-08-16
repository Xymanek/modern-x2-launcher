using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using ModernX2Launcher.Utilities;
using ModernX2Launcher.ViewModels.Common;
using ModernX2Launcher.ViewModels.MainWindow.ModList;
using ModernX2Launcher.ViewModels.MainWindow.Overrides;
using ModernX2Launcher.ViewModels.MainWindow.Profiles;
using ModernX2Launcher.ViewModels.MainWindow.Updates;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels.MainWindow;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        Modes = new IMainWindowMode[]
        {
            ModListMode,
            OverridesMode,
            UpdatesMode,
            ProfilesMode,
        };

        _activeMode = ModListMode;
        this.RaisePropertyChanged(nameof(ActiveMode));

        IMenuItemViewModel[] commonMenuItems = CreateCommonMenuItems();

        _menuItems = this.WhenAnyValue(m => m.ActiveMode)
            .Select(
                mode => mode.AdditionalMenuItems
                    .DefaultIfEmpty(Array.Empty<IMenuItemViewModel>())
            )
            .Switch()
            .Select(additionalItems => commonMenuItems.Concat(additionalItems).ToReadOnlyList())
            .ToProperty(this, nameof(MenuItems));
    }

    private IMenuItemViewModel[] CreateCommonMenuItems()
    {
        return new IMenuItemViewModel[] 
        {
            new MenuItemViewModel
            {
                Header = "_File",
                Items = new IMenuItemViewModel[]
                {
                    new MenuItemViewModel { Header = "_Open..." },

                    new MenuItemSeparatorViewModel(),

                    new MenuItemViewModel { Header = "_Exit" },
                }
            },
            new MenuItemViewModel
            {
                Header = "_Edit",
                Items = new[]
                {
                    new MenuItemViewModel { Header = "_Copy" },
                    new MenuItemViewModel { Header = "_Paste" },
                }
            }
        };
    }

    private readonly ObservableAsPropertyHelper<IReadOnlyList<IMenuItemViewModel>> _menuItems;
    public IReadOnlyList<IMenuItemViewModel> MenuItems => _menuItems.Value;

    public ModListModeViewModel ModListMode { get; } = new();
    public OverridesModeViewModel OverridesMode { get; } = new();
    public UpdatesModeViewModel UpdatesMode { get; } = new();
    public ProfilesModeViewModel ProfilesMode { get; } = new();

    public IReadOnlyList<IMainWindowMode> Modes { get; }

    private IMainWindowMode _activeMode;

    public IMainWindowMode ActiveMode
    {
        get => _activeMode;
        set => this.RaiseAndSetIfChanged(ref _activeMode, value);
    }
}