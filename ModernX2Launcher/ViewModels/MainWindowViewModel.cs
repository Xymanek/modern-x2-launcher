using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using ModernX2Launcher.Utilities;
using ModernX2Launcher.ViewModels.MainWindowModes;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        Modes = new IMainWindowMode[]
        {
            ModListMode,
            ProfileMode
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
    public ProfileModeViewModel ProfileMode { get; } = new();

    public IReadOnlyList<IMainWindowMode> Modes { get; }

    private IMainWindowMode _activeMode;

    public IMainWindowMode ActiveMode
    {
        get => _activeMode;
        set => this.RaiseAndSetIfChanged(ref _activeMode, value);
    }
}