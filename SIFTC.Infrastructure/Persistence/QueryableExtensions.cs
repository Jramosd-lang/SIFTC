using System.Linq.Expressions;
using SIFTC.Application.Common;

namespace SIFTC.Infrastructure.Persistence;

public static class QueryableExtensions
{
    public static IOrderedQueryable<T> OrdenarPor<T, TKey>(this IQueryable<T> query, Expression<Func<T, TKey>> clave, DireccionOrden direccion)
    {
        return direccion == DireccionOrden.Desc ? query.OrderByDescending(clave) : query.OrderBy(clave);
    }

    public static IOrderedQueryable<T> EntoncesPor<T, TKey>(this IOrderedQueryable<T> query, Expression<Func<T, TKey>> clave, DireccionOrden direccion)
    {
        return direccion == DireccionOrden.Desc ? query.ThenByDescending(clave) : query.ThenBy(clave);
    }
}
