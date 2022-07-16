﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ModListViewModel _modList = new();
    private ModInfoViewModel _modInfo = new();

    public MainWindowViewModel()
    {
        DesignTimeModListViewModel.PopulateDummy(_modList);

        _modList.SelectedMods.Connect()
            .Select(changeSet =>
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
            })
            .WhereNotNull() // Keep last selection
            .Subscribe(modEntry => ModInfo.ModEntry = modEntry); // TODO: unsub
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
}