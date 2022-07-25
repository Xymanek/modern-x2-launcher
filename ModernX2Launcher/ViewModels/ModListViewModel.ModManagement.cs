using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using DynamicData;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels;

public partial class ModListViewModel
{
    public sealed class SetSelectedModsCategoryOption
    {
        private readonly ModListViewModel _listViewModel;
        private readonly string _category;

        public SetSelectedModsCategoryOption(string category, ModListViewModel listViewModel)
        {
            _listViewModel = listViewModel;
            _category = category;

            Command = ReactiveCommand.Create(
                OnSelected,
                listViewModel.WhenAnySelectedModsAre(mod => mod.Category, modCategory => modCategory != _category)
            );
        }

        private void OnSelected()
        {
            foreach (ModEntryViewModel mod in _listViewModel.SelectedMods.Items)
            {
                mod.Category = _category;
            }
        }

        public string Label => _category;

        public ReactiveCommand<Unit, Unit> Command { get; }
    }

    public IObservable<IChangeSet<SetSelectedModsCategoryOption>> SetSelectedModsCategoryOptionStream { get; }

    public ReactiveCommand<ModEntryViewModel, Unit> ToggleModEnabled { get; }

    private void OnToggleModEnabled(ModEntryViewModel interactedMod)
    {
        bool newIsEnabled = !interactedMod.IsEnabled;

        // If we click the checkbox on one of the enabled mods, then do the same change to all of them
        IEnumerable<ModEntryViewModel> modsToUpdate = SelectedMods.Items.Contains(interactedMod)
            ? SelectedMods.Items
            : new[] { interactedMod };

        foreach (ModEntryViewModel mod in modsToUpdate)
        {
            mod.IsEnabled = newIsEnabled;
        }
    }

    public ReactiveCommand<Unit, Unit> EnableSelectedMods { get; }

    private void OnEnableSelectedMods()
    {
        foreach (ModEntryViewModel mod in SelectedMods.Items)
        {
            mod.IsEnabled = true;
        }
    }
}