using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Application.Interfaces.DataAccess;

public interface IBaseReadRepository<T>
    where T : class
{
    Task<T?> FindById(Guid id);

    Task<T?> Search(params object[] keyValues);

    Task<T?> Single(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        bool disableTracking = true);

    Task<IQueryable<T>> QueryAll(bool disableTracking = true);

    Task<IQueryable<T>> QueryCondition(
        Expression<Func<T, bool>> expression,
        bool disableTracking = true);

    Task<bool> Any(Expression<Func<T, bool>> expression);

    Task<IQueryable<TType>> Select<TType>(Expression<Func<T, TType>> select);
}