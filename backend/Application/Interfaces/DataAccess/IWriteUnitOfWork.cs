using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces.DataAccess;

public interface IWriteUnitOfWork : IDisposable
{
    DbContext DbContext { get; }
    Task<int> SaveChanges();

    IBaseWriteRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class;
}