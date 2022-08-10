using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using ModernX2Launcher.ViewModels.MainWindowModes;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        IMenuItemViewModel[] commonMenuItems =
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

        // TODO: connect to mode switching
        _menuItems = ModListMode.AdditionalMenuItems
            .Select(additionalItems =>
            {
                IEnumerable<IMenuItemViewModel> finalItems = commonMenuItems;

                if (additionalItems != null)
                {
                    finalItems = finalItems.Concat(additionalItems);
                }

                return (IReadOnlyList<IMenuItemViewModel>) finalItems.ToArray();
            })
            .ToProperty(this, nameof(MenuItems));
    }

    private readonly ObservableAsPropertyHelper<IReadOnlyList<IMenuItemViewModel>> _menuItems;
    public IReadOnlyList<IMenuItemViewModel> MenuItems => _menuItems.Value;
    
    public ModListModeViewModel ModListMode { get; } = new();
}