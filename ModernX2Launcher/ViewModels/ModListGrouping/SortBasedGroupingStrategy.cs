using Avalonia.Collections;
using ModernX2Launcher.Utilities.SortStack;

namespace ModernX2Launcher.ViewModels.ModListGrouping;

public class SortBasedGroupingStrategy : IGroupingStrategy
{
    public IRefreshableSorter<ModEntryViewModel>? GetPrimarySorter()
    {
        throw new System.NotImplementedException();
    }

    public bool ShouldSkipSortDescription(DataGridSortDescription sortDescription)
    {
        throw new System.NotImplementedException();
    }

    public DataGridGroupDescription? GetGroupDescription()
    {
        throw new System.NotImplementedException();
    }
}