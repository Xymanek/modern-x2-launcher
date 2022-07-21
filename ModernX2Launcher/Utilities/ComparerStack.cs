using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ModernX2Launcher.Utilities;

public class ComparerStack<T> : IComparer<T>
{
    private readonly IReadOnlyList<IComparer<T>> _innerComparers;

    public ComparerStack(IEnumerable<IComparer<T>> innerComparers)
    {
        _innerComparers = innerComparers.ToArray();
        
        Debug.Assert(_innerComparers.Count > 0);
    }

    public int Compare(T? x, T? y)
    {
        return _innerComparers
            .Select(comparer => comparer.Compare(x, y))
            .FirstOrDefault(result => result != 0, 0);
    }
}

public static class ComparerStackExtensions
{
    // TODO: might exist a better signature (e.g. ToMerged -> IComparer) 
    public static ComparerStack<T> ToStack<T>(this IEnumerable<IComparer<T>> comparers) => new(comparers);
}