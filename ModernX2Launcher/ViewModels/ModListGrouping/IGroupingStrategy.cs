using Avalonia.Collections;
using ModernX2Launcher.Utilities.SortStack;

namespace ModernX2Launcher.ViewModels.ModListGrouping;

// TODO: convert to return Observable 
public interface IGroupingStrategy
{
    IRefreshableSorter<ModEntryViewModel>? GetPrimarySorter();
    
    bool ShouldSkipSortDescription(DataGridSortDescription sortDescription);

    DataGridGroupDescription? GetGroupDescription();
}