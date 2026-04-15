using System.Linq.Expressions;

namespace backend.Extensions
{
    public static class QueryableSortingExtensions
    {
        public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, string? sortBy, bool descending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query; //default ordering handled in repo if needed

            //Try to get property by name
            var prop = typeof(T).GetProperty(sortBy,
                        System.Reflection.BindingFlags.IgnoreCase |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Instance);
            if (prop == null)
                return query; //fallback if property not found

            var param = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.MakeMemberAccess(param, prop);
            var orderByExp = Expression.Lambda(propertyAccess, param);

            string method = descending ? "OrderByDescending" : "OrderBy";

            var resultExp = Expression.Call(
                typeof(Queryable),
                method,
                new Type[] { typeof(T), prop.PropertyType },
                query.Expression,
                Expression.Quote(orderByExp));

            return query.Provider.CreateQuery<T>(resultExp);
        }
    }
}
