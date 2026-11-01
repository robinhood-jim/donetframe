using System;
using System.Linq;
using System.Linq.Expressions;

namespace Frameset.Core.Linq;

public static class LambdaQueryExtensions
{
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition,
        Expression<Func<T, bool>> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }
    public static IQueryable<T> OrderByIf<T, TKey>(this IQueryable<T> source, bool condition, Expression<Func<T, TKey>> keySelector)
    {
        return condition ? source.OrderBy(keySelector) : source;
    }

    public static IQueryable<T> OrderByDescendingIf<T, TKey>(this IQueryable<T> source, bool condition, Expression<Func<T, TKey>> keySelector)
    {
        return condition ? source.OrderByDescending(keySelector) : source;
    }

    // Handles nested group conditions: .Where(u => ...) or .Where(q => q.Or(...))
    public static IQueryable<T> AndGroupIf<T>(this IQueryable<T> source, bool condition, Action<QueryGroupWrapper<T>> groupAction)
    {
        if (!condition) return source;
        var wrapper = new QueryGroupWrapper<T>("AND");
        groupAction(wrapper);
        var expression = wrapper.BuildExpression();
        return expression != null ? source.Where(expression) : source;
    }

    public static IQueryable<T> OrGroupIf<T>(this IQueryable<T> source, bool condition, Action<QueryGroupWrapper<T>> groupAction)
    {
        if (!condition) return source;
        var wrapper = new QueryGroupWrapper<T>("OR");
        groupAction(wrapper);
        var expression = wrapper.BuildExpression();
        return expression != null ? source.Where(expression) : source;
    }
}