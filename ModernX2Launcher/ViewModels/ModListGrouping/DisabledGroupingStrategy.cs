using Avalonia.Collections;
using ModernX2Launcher.Utilities.SortStack;

namespace ModernX2Launcher.ViewModels.ModListGrouping;

public class DisabledGroupingStrategy : IGroupingStrategy
{
    public IRefreshableSorter<ModEntryViewModel>? GetPrimarySorter() => null;

    public bool ShouldSkipSortDescription(DataGridSortDescription sortDescription) => false;

    public DataGridGroupDescription? GetGroupDescription() => null;
}