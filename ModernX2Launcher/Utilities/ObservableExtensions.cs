using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace ModernX2Launcher.Utilities;

public static class ObservableExtensions
{
    public static IObservable<T?> SelectFirstOrDefault<T>(this IObservable<IEnumerable<T>> source)
        => source.Select(enumerable => enumerable.FirstOrDefault());
}