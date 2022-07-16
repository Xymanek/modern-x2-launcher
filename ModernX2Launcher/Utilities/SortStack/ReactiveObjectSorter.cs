using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;

namespace ModernX2Launcher.Utilities.SortStack;

public class ReactiveObjectSorter<TReactiveObject, TProperty, TDerived>
    : BidirectionalSorterBase<TReactiveObject, TDerived>, IRefreshableSorter<TReactiveObject>
    where TReactiveObject : ReactiveObject
{
    private readonly Expression<Func<TReactiveObject, TProperty>> _propertyExpression;
    private readonly Func<TProperty, TDerived> _derivedSelector;
    private readonly IEqualityComparer<TDerived> _equalityComparer;
    private readonly IComparer<TDerived> _comparer;

    private readonly Func<TReactiveObject, TProperty> _propertyAccessor;

    public ReactiveObjectSorter(
        Expression<Func<TReactiveObject, TProperty>> propertyExpression,
        Func<TProperty, TDerived> derivedSelector,
        IEqualityComparer<TDerived> equalityComparer,
        IComparer<TDerived> comparer,
        bool isDescending
    )
    {
        _propertyExpression = propertyExpression;
        _derivedSelector = derivedSelector;
        _equalityComparer = equalityComparer;
        _comparer = comparer;

        _propertyAccessor = _propertyExpression.Compile();

        IsDescending = isDescending;
    }

    public override bool IsDescending { get; }

    protected override TDerived GetSortKey(TReactiveObject entry)
    {
        TProperty propertyValue = _propertyAccessor(entry);

        return _derivedSelector(propertyValue);
    }

    protected override IComparer<TDerived> GetComparer() => _comparer;

    public IObservable<Unit> GetResortObservable(TReactiveObject reactiveObject)
    {
        return reactiveObject.WhenAnyValue(_propertyExpression)
            .DistinctUntilChanged(_derivedSelector, _equalityComparer)
            .Skip(1) // Do not resort on the initial value emit (DUC forwards the first value without filtering)
            .Select(_ => Unit.Default);
    }

    public override IBidirectionalSorter<TReactiveObject> GetReversed()
    {
        return new ReactiveObjectSorter<TReactiveObject, TProperty, TDerived>(
            _propertyExpression, _derivedSelector, _equalityComparer, _comparer,
            !IsDescending
        );
    }
}

public static class ReactiveObjectSorterFor<TReactiveObject>
    where TReactiveObject : ReactiveObject
{
    public static ReactiveObjectSorter<TReactiveObject, TProperty, TProperty> Create<TProperty>(
        Expression<Func<TReactiveObject, TProperty>> propertyExpression,
        bool isDescending = false
    )
    {
        return new ReactiveObjectSorter<TReactiveObject, TProperty, TProperty>(
            propertyExpression,
            property => property,
            EqualityComparer<TProperty>.Default,
            Comparer<TProperty>.Default,
            isDescending
        );
    }
}