namespace Application.Interfaces.DataAccess;

public interface IBaseWriteRepository<T> : IBaseReadRepository<T>
    where T : class
{
    Task Add(T entity);

    Task Add(params T[] entities);

    Task Add(IEnumerable<T> entities);

    Task Update(T entity);

    Task Update(params T[] entities);

    Task Update(IEnumerable<T> entities);

    Task Delete(object[] keyValues);

    Task Delete(object id);

    Task Delete(T entity);

    Task Delete(params T[] entities);

    Task Delete(IEnumerable<T> entities);
}