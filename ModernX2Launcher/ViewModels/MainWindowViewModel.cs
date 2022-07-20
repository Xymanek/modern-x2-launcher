using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using ModernX2Launcher.ModDiscovery;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ModListViewModel _modList = new();
    private ModInfoViewModel _modInfo = new();

    public MainWindowViewModel()
    {
        DesignTimeModListViewModel.PopulateDummy(_modList);

        TestStuff = ReactiveCommand.CreateFromTask(DoTestStuff);

        this.WhenActivated(disposables =>
        {
            _modList.SelectedMods.Connect()
                .Select(GetLastSelectedMod)
                .WhereNotNull() // Keep last selection
                .Subscribe(modEntry => ModInfo.ModEntry = modEntry)
                .DisposeWith(disposables);
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

    public ModListViewModel ModList
    {
        get => _modList;
        set => this.RaiseAndSetIfChanged(ref _modList, value);
    }

    public ModInfoViewModel ModInfo
    {
        get => _modInfo;
        set => this.RaiseAndSetIfChanged(ref _modInfo, value);
    }

    public ReactiveCommand<Unit, Unit> TestStuff { get; }

    private async Task DoTestStuff()
    {
        ModRootCandidateDiscoverer candidateDiscoverer = new();

        IReadOnlyList<ModRootCandidate> candidates = await candidateDiscoverer.DiscoverCandidatesAsync(
            "C:\\Steam\\steamapps\\common\\XCOM 2\\XCom2-WarOfTheChosen\\XComGame\\Mods"
        );
    }
}