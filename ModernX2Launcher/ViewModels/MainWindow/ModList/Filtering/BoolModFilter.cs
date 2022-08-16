using System;
using System.Linq.Expressions;
using System.Reactive.Linq;
using ModernX2Launcher.ViewModels.Common;
using ReactiveUI;

namespace ModernX2Launcher.ViewModels.MainWindow.ModList.Filtering;

public enum BoolModFilterKind
{
    ExcludeTrue,
    ExcludeFalse,
}

public abstract class BoolModFilter : IModListFilter
{
    protected BoolModFilter(BoolModFilterKind kind)
    {
        Kind = kind;
    }

    public BoolModFilterKind Kind { get; }

    public string DisplayLabel
    {
        get
        {
            string kindString = Kind switch
            {
                BoolModFilterKind.ExcludeTrue => "NOT",
                BoolModFilterKind.ExcludeFalse => "IS",

                _ => throw new Exception($"{nameof(BoolModFilter)}.{nameof(Kind)} contains unknown value"),
            };

            return $"{kindString} {PropertyDisplayName}";
        }
    }

    protected abstract string PropertyDisplayName { get; }

    public IObservable<bool> DoesModPass(ModEntryViewModel mod)
    {
        IObservable<bool> valueObs = mod.WhenAnyValue(ValueGetterExpression);

        return Kind switch
        {
            BoolModFilterKind.ExcludeFalse => valueObs,
            BoolModFilterKind.ExcludeTrue => valueObs.Select(b => !b),

            _ => throw new Exception($"{nameof(BoolModFilter)}.{nameof(Kind)} contains unknown value"),
        };
    }

    protected abstract Expression<Func<ModEntryViewModel, bool>> ValueGetterExpression { get; }
}

public class IsEnabledFilter : BoolModFilter
{
    public IsEnabledFilter(BoolModFilterKind kind) : base(kind)
    {
    }

    protected override string PropertyDisplayName => "Enabled";

    protected override Expression<Func<ModEntryViewModel, bool>> ValueGetterExpression
        => mod => mod.IsEnabled;
}