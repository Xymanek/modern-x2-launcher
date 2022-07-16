using System.Collections.Generic;
using System.Linq;

namespace ModernX2Launcher.Utilities.SortStack;

public abstract class SorterBase<TEntry, TSortKey> : ISorter<TEntry>
{
    public IOrderedEnumerable<TEntry> SortEnumerable(IEnumerable<TEntry> enumerable)
    {
        IComparer<TSortKey> comparer = GetComparer();
        
        if (enumerable is IOrderedEnumerable<TEntry> orderedEnumerable)
        {
            return orderedEnumerable.CreateOrderedEnumerable(GetSortKey, comparer, false);
        }
        
        return enumerable.OrderBy(GetSortKey, comparer);
    }
    
    protected abstract TSortKey GetSortKey(TEntry entry);
    protected abstract IComparer<TSortKey> GetComparer();
}

public abstract class BidirectionalSorterBase<TEntry, TSortKey> : IBidirectionalSorter<TEntry>
{
    public IOrderedEnumerable<TEntry> SortEnumerable(IEnumerable<TEntry> enumerable)
    {
        IComparer<TSortKey> comparer = GetComparer();
        
        if (enumerable is IOrderedEnumerable<TEntry> orderedEnumerable)
        {
            return orderedEnumerable.CreateOrderedEnumerable(GetSortKey, comparer, IsDescending);
        }
        
        return IsDescending
            ? enumerable.OrderByDescending(GetSortKey, comparer)
            : enumerable.OrderBy(GetSortKey, comparer);
    }

    protected abstract TSortKey GetSortKey(TEntry entry);
    protected abstract IComparer<TSortKey> GetComparer();

    public abstract bool IsDescending { get; }
    public abstract IBidirectionalSorter<TEntry> GetReversed();
}