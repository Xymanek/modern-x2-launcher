using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using DynamicData;
using ModernX2Launcher.Utilities;
using ModernX2Launcher.ViewModels.Common;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels.MainWindow.ModList;

public partial class ModListViewModel
{
    public SourceList<ModEntryViewModel> SelectedMods { get; } = new();

    private IObservable<bool> WhenAnySelectedModsAre(
        Expression<Func<ModEntryViewModel, bool>> propertyExpression
    )
    {
        return WhenAnySelectedModsAre(propertyExpression, b => b);
    }

    private IObservable<bool> WhenAnySelectedModsAreNot(
        Expression<Func<ModEntryViewModel, bool>> propertyExpression
    )
    {
        return WhenAnySelectedModsAre(propertyExpression, b => !b);
    }

    private IObservable<bool> WhenAnySelectedModsAre<TProperty>(
        Expression<Func<ModEntryViewModel, TProperty>> propertyExpression,
        Func<TProperty, bool> propertyEvaluator
    )
    {
        return WhenSelectedModsAre(propertyExpression, values => values.Any(propertyEvaluator));
    }

    private IObservable<bool> WhenSelectedModsAre<TProperty>(
        Expression<Func<ModEntryViewModel, TProperty>> propertyExpression,
        Func<IList<TProperty>, bool> propertyValuesEvaluator
    )
    {
        return SelectedMods.Connect()
            .Snapshots()
            .Select(selectedMods =>
            {
                // If no mods are selected, then we can't preform any operation on selected mods (duh) 
                if (selectedMods.Count < 1)
                {
                    return Observable.Return(false);
                }

                return selectedMods
                    .Select(mod => mod.WhenAnyValue(propertyExpression))
                    .CombineLatest(propertyValuesEvaluator);
            })
            .Switch();
    }
}