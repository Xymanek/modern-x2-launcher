using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Material.Icons;
using ModernX2Launcher.ModDiscovery;
using ModernX2Launcher.Utilities;
using ModernX2Launcher.ViewModels.Common;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels.MainWindow.ModList;

public class ModListModeViewModel : ViewModelBase, IMainWindowMode
{
    public ModListModeViewModel()
    {
        DummyMods.Populate(ModList);

        TestStuff = ReactiveCommand.CreateFromTask(DoTestStuff);

        this.WhenActivated(disposables =>
        {
            ModList.SelectedMods.Connect()
                .Snapshots()
                .Subscribe(selectedMods =>
                {
                    ModInfo.MultipleSelected = selectedMods.Count > 1;
                    
                    if (selectedMods.Count == 1)
                    {
                        ModInfo.ModEntry = selectedMods.First();
                        return;
                    }

                    ModInfo.ModEntry = null;
                })
                .DisposeWith(disposables);
        });

        AdditionalMenuItems = Observable.Return(new[]
        {
            new MenuItemViewModel
            {
                Header = "Mod _List",
                Items = new[]
                {
                    ModList.GroupingMenuItem,
                }
            },
        });
    }

    public ModListViewModel ModList { get; } = new();

    public ModInfoViewModel ModInfo { get; } = new();

    public ReactiveCommand<Unit, Unit> TestStuff { get; }

    private async Task DoTestStuff()
    {
        ModRootCandidateDiscoverer candidateDiscoverer = new();

        IReadOnlyList<ModRootCandidate> candidates = await candidateDiscoverer.DiscoverCandidatesAsync(
            "C:\\Steam\\steamapps\\common\\XCOM 2\\XCom2-WarOfTheChosen\\XComGame\\Mods"
        );
    }

    public string ModeName => "Mods";
    public MaterialIconKind ModeIcon => MaterialIconKind.FormatListChecks;

    public IObservable<IReadOnlyList<IMenuItemViewModel>> AdditionalMenuItems { get; }
}