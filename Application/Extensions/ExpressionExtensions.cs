using System.Linq.Expressions;

namespace Application.Extensions;

public static class ExpressionExtensions
{
    /// <summary>
    ///     与条件组合 (AND)
    /// </summary>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(expr1.Body, invokedExpr),
            expr1.Parameters);
    }

    /// <summary>
    ///     或条件组合 (OR)
    /// </summary>
    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(expr1.Body, invokedExpr),
            expr1.Parameters);
    }
}