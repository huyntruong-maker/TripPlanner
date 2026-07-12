using System.Linq.Expressions;
using Application.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Infrastructure.DataAccess.Repositories;

public class BaseReadRepository<T>(DbContext context) : IBaseReadRepository<T>
    where T : class
{
    protected DbSet<T> DbSet { get; } = context.Set<T>();

    protected DbContext DbContext { get; } = context ?? throw new ArgumentException(nameof(context));

    public async Task<T?> FindById(Guid id)
    {
        return await DbSet.FindAsync(id);
    }

    public async Task<T?> Search(params object[] keyValues)
    {
        return await DbSet.FindAsync(keyValues);
    }

    public async Task<T?> Single(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        bool disableTracking = true)
    {
        IQueryable<T> query = DbSet;
        if (disableTracking) query = query.AsNoTracking();

        if (include != null) query = include(query);

        if (predicate != null) query = query.Where(predicate);

        if (orderBy != null) return await orderBy(query).FirstOrDefaultAsync();

        return await query.FirstOrDefaultAsync();
    }

    public async Task<IQueryable<T>> QueryAll(bool disableTracking = true)
    {
        var queryable = disableTracking ? DbSet.AsNoTracking() : DbSet;
        return await Task.FromResult(queryable);
    }

    public async Task<IQueryable<T>> QueryCondition(
        Expression<Func<T, bool>> expression,
        bool disableTracking = true)
    {
        var queryable = disableTracking ? DbSet.Where(expression).AsNoTracking() : DbSet.Where(expression);
        return await Task.FromResult(queryable);
    }

    public async Task<bool> Any(Expression<Func<T, bool>> expression)
    {
        return await DbSet.AnyAsync(expression);
    }

    public async Task<IQueryable<TType>> Select<TType>(Expression<Func<T, TType>> select)
    {
        return await Task.FromResult(DbSet.Select(select));
    }
}