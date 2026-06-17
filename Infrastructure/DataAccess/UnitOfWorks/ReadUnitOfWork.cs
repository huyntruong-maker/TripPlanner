using Application.Interfaces.DataAccess;
using Infrastructure.DataAccess.DbContexts;
using Infrastructure.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess.UnitOfWorks;

public class ReadUnitOfWork : IReadUnitOfWork
{
    private readonly DbContext _context;
    private readonly Dictionary<Type, object> _repositories;

    public ReadUnitOfWork(ReadDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _repositories = new Dictionary<Type, object>();
        DbContext = _context;
    }

    public DbContext DbContext { get; }

    public IBaseReadRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class
    {
        var type = typeof(BaseReadRepository<TEntity>);

        if (!_repositories.TryGetValue(type, out var value))
        {
            value = new BaseReadRepository<TEntity>(_context);
            _repositories[type] = value;
        }

        return (IBaseReadRepository<TEntity>)value;
    }
    
    public void ChangeDatabase(string connectionString)
    {
        _context.Database.SetConnectionString(connectionString);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}