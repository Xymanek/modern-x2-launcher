using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace ModernX2Launcher.Utilities;

public static class EnumerableExtensions
{
    [Pure]
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : class
    {
        return source.Where(item => item is not null)!;
    }

    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source)
    {
        // TODO: wrap so cannot be downcasted
        return source.ToArray();
    }
}