using System.Reflection;
using Application.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess.Repositories;

public class BaseWriteRepository<T>(DbContext context) : BaseReadRepository<T>(context), IBaseWriteRepository<T>
    where T : class
{
    public async Task Add(T entity)
    {
        await DbSet.AddAsync(entity);
    }

    public async Task Add(params T[] entities)
    {
        await DbSet.AddRangeAsync(entities);
    }

    public async Task Add(IEnumerable<T> entities)
    {
        await DbSet.AddRangeAsync(entities);
    }

    public async Task Update(T entity)
    {
        DbSet.Update(entity);
        await Task.CompletedTask;
    }

    public async Task Update(params T[] entities)
    {
        DbSet.UpdateRange(entities);
        await Task.CompletedTask;
    }

    public async Task Update(IEnumerable<T> entities)
    {
        DbSet.UpdateRange(entities);
        await Task.CompletedTask;
    }

    public async Task Delete(object[] keyValues)
    {
        var entity = await DbSet.FindAsync(keyValues);

        if (entity != null) DbSet.Remove(entity);
    }

    public async Task Delete(object id)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity != null) DbSet.Remove(entity);
    }

    public async Task Delete(T entity)
    {
        var typeInfo = typeof(T).GetTypeInfo();
        var key = DbContext.Model.FindEntityType(typeInfo)!.FindPrimaryKey()!.Properties.FirstOrDefault();
        var id = entity.GetType().GetProperty(key?.Name ?? string.Empty)?.GetValue(entity);
        if (id == null) return;

        await Delete(id);
    }

    public async Task Delete(params T[] entities)
    {
        DbSet.RemoveRange(entities);
        await Task.CompletedTask;
    }

    public async Task Delete(IEnumerable<T> entities)
    {
        DbSet.RemoveRange(entities);
        await Task.CompletedTask;
    }
}