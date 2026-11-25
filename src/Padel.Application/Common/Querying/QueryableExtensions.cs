using System.Linq.Expressions;
using Padel.Domain.Common;

namespace Padel.Application.Common.Querying;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, ListOptions<T> options) where T : Entity =>
        options.Filters.Aggregate(query, (current, filter) => current.Where(filter));

    /// <summary>
    /// Applies the requested sort order, with the id as final tie-breaker so every item has its own position.
    /// </summary>
    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, ListOptions<T> options) where T : Entity
    {
        IOrderedQueryable<T>? ordered = null;

        foreach (var sort in options.Sorts)
        {
            var key = sort.Field.OrderKey;
            ordered = (ordered, sort.Descending) switch
            {
                (null, false) => query.OrderBy(key),
                (null, true) => query.OrderByDescending(key),
                (_, false) => ordered.ThenBy(key),
                (_, true) => ordered.ThenByDescending(key),
            };
        }

        return ordered is null ? query.OrderBy(e => e.Id) : ordered.ThenBy(e => e.Id);
    }

    /// <summary>
    /// Keyset paging: the items after the cursor position, plus one extra to know whether there is a next page
    /// (see <see cref="PagedResult.FromWindow"/>). Apply after <see cref="ApplySorting"/>.
    /// </summary>
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, ListOptions<T> options) where T : Entity
    {
        if (options.After is { } after)
        {
            query = query.Where(After(options.Sorts, after));
        }

        return query.Take(options.PageSize + 1);
    }

    /// <summary>
    /// For sort keys (k1, k2) and the id: <c>k1 &gt; v1 OR (k1 = v1 AND k2 &gt; v2) OR (k1 = v1 AND k2 = v2 AND id &gt; vid)</c>,
    /// with <c>&lt;</c> for descending keys. The values are parameters, so EF Core reuses the query plan.
    /// </summary>
    private static Expression<Func<T, bool>> After<T>(IReadOnlyList<SortExpression<T>> sorts, CursorPosition after) where T : Entity
    {
        var entity = Expression.Parameter(typeof(T), "entity");

        var keys = sorts
            .Select((sort, index) => (
                Key: ParameterReplacer.Replace(sort.Field.Key, entity),
                sort.Descending,
                Value: Parameter(after.Keys[index], sort.Field.KeyType)))
            .Append((Key: Expression.Property(entity, nameof(Entity.Id)), Descending: false, Value: Parameter(after.Id, typeof(Guid))));

        Expression? predicate = null;
        Expression? equalSoFar = null;

        foreach (var (key, descending, value) in keys)
        {
            var beyond = Compare(key, value, descending);
            var term = equalSoFar is null ? beyond : Expression.AndAlso(equalSoFar, beyond);
            predicate = predicate is null ? term : Expression.OrElse(predicate, term);

            var equal = Expression.Equal(key, value);
            equalSoFar = equalSoFar is null ? equal : Expression.AndAlso(equalSoFar, equal);
        }

        return Expression.Lambda<Func<T, bool>>(predicate!, entity);
    }

    /// <summary><c>key &gt; value</c> (or <c>&lt;</c>); strings with <c>string.Compare</c>, which EF Core translates to SQL.</summary>
    private static BinaryExpression Compare(Expression key, Expression value, bool descending)
    {
        if (key.Type == typeof(string))
        {
            var comparison = Expression.Call(typeof(string), nameof(string.Compare), null, key, value);
            var zero = Expression.Constant(0);
            return descending ? Expression.LessThan(comparison, zero) : Expression.GreaterThan(comparison, zero);
        }

        return descending ? Expression.LessThan(key, value) : Expression.GreaterThan(key, value);
    }

    /// <summary>The value as a closure field, like a captured variable, so it becomes a SQL parameter.</summary>
    private static MemberExpression Parameter(object value, Type type)
    {
        var holder = Activator.CreateInstance(typeof(ValueHolder<>).MakeGenericType(type), value)!;
        return Expression.Field(Expression.Constant(holder), nameof(ValueHolder<>.Value));
    }

    private sealed class ValueHolder<TValue>(TValue value)
    {
        public readonly TValue Value = value;
    }

    private sealed class ParameterReplacer(ParameterExpression from, Expression to) : ExpressionVisitor
    {
        public static Expression Replace(LambdaExpression lambda, ParameterExpression parameter) =>
            new ParameterReplacer(lambda.Parameters[0], parameter).Visit(lambda.Body);

        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}
