using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using Material.Icons;
using ModernX2Launcher.ModDiscovery;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels.MainWindowModes;

public class ModListModeViewModel : ViewModelBase, IMainWindowMode
{
    public ModListModeViewModel()
    {
        DummyMods.Populate(ModList);

        TestStuff = ReactiveCommand.CreateFromTask(DoTestStuff);

        this.WhenActivated(disposables =>
        {
            ModList.SelectedMods.Connect()
                .Select(GetLastSelectedMod)
                .WhereNotNull() // Keep last selection
                .Subscribe(modEntry => ModInfo.ModEntry = modEntry)
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

    private static ModEntryViewModel? GetLastSelectedMod(IChangeSet<ModEntryViewModel> changeSet)
    {
        IEnumerable<ModEntryViewModel> GetAllAdded()
        {
            foreach (Change<ModEntryViewModel> change in changeSet)
            {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (change.Reason)
                {
                    case ListChangeReason.Add:
                        yield return change.Item.Current;
                        break;

                    case ListChangeReason.AddRange:
                        foreach (ModEntryViewModel modEntry in change.Range)
                        {
                            yield return modEntry;
                        }

                        break;
                }
            }
        }

        return GetAllAdded().LastOrDefault();
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