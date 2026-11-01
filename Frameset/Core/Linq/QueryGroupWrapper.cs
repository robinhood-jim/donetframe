using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace Frameset.Core.Linq;

public class QueryGroupWrapper<T>
{
    private readonly string _groupOperator; // "AND" or "OR"
    private Expression<Func<T, bool>> _combinedExpression = null!;

    public QueryGroupWrapper(string groupOperator) => _groupOperator = groupOperator;

    public QueryGroupWrapper<T> Eq<TProperty>(bool condition, Expression<Func<T, TProperty>> property, TProperty value)
    {
        if (!condition) return this;
        Expression equalExpr = Expression.Equal(property.Body, Expression.Constant(value, typeof(TProperty)));
        AppendExpression(Expression.Lambda<Func<T, bool>>(equalExpr, property.Parameters));
        return this;
    }
    public QueryGroupWrapper<T> Ne<TProperty>(bool condition, Expression<Func<T, TProperty>> property, TProperty value)
    {
        if (!condition) return this;
        Expression equalExpr = Expression.NotEqual(property.Body, Expression.Constant(value, typeof(TProperty)));
        AppendExpression(Expression.Lambda<Func<T, bool>>(equalExpr, property.Parameters));
        return this;
    }
    public QueryGroupWrapper<T> Gt<TProperty>(bool condition, Expression<Func<T, TProperty>> property, TProperty value)
    {
        if (!condition) return this;
        Expression equalExpr = Expression.GreaterThan(property.Body, Expression.Constant(value, typeof(TProperty)));
        AppendExpression(Expression.Lambda<Func<T, bool>>(equalExpr, property.Parameters));
        return this;
    }
    public QueryGroupWrapper<T> Ge<TProperty>(bool condition, Expression<Func<T, TProperty>> property, TProperty value)
    {
        if (!condition) return this;
        Expression equalExpr = Expression.GreaterThanOrEqual(property.Body, Expression.Constant(value, typeof(TProperty)));
        AppendExpression(Expression.Lambda<Func<T, bool>>(equalExpr, property.Parameters));
        return this;
    }
    public QueryGroupWrapper<T> Lt<TProperty>(bool condition, Expression<Func<T, TProperty>> property, TProperty value)
    {
        if (!condition) return this;
        Expression equalExpr = Expression.LessThan(property.Body, Expression.Constant(value, typeof(TProperty)));
        AppendExpression(Expression.Lambda<Func<T, bool>>(equalExpr, property.Parameters));
        return this;
    }
    public QueryGroupWrapper<T> Le<TProperty>(bool condition, Expression<Func<T, TProperty>> property, TProperty value)
    {
        if (!condition) return this;
        Expression equalExpr = Expression.LessThanOrEqual(property.Body, Expression.Constant(value, typeof(TProperty)));
        AppendExpression(Expression.Lambda<Func<T, bool>>(equalExpr, property.Parameters));
        return this;
    }

    public QueryGroupWrapper<T> Like(bool condition, Expression<Func<T, string>> property, string value)
    {
        if (!condition || string.IsNullOrEmpty(value)) return this;
        var method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        Trace.Assert(method!=null,"");
        var methodCall = Expression.Call(property.Body, method, Expression.Constant(value));
        AppendExpression(Expression.Lambda<Func<T, bool>>(methodCall, property.Parameters));
        return this;
    }

    private void AppendExpression(Expression<Func<T, bool>> newExpr)
    {
        if (_combinedExpression == null)
        {
            _combinedExpression = newExpr;
            return;
        }

        var invokedExpr = Expression.Invoke(newExpr, _combinedExpression.Parameters.Cast<Expression>());
        var body = _groupOperator == "OR"
            ? Expression.OrElse(_combinedExpression.Body, invokedExpr)
            : Expression.AndAlso(_combinedExpression.Body, invokedExpr);

        _combinedExpression = Expression.Lambda<Func<T, bool>>(body, _combinedExpression.Parameters);
    }

    public Expression<Func<T, bool>> BuildExpression() => _combinedExpression;
}