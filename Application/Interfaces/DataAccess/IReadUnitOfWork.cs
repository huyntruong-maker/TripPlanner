using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces.DataAccess;

public interface IReadUnitOfWork : IDisposable
{
    DbContext DbContext { get; }

    IBaseReadRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class;
    
    void ChangeDatabase(string connectionString);
}