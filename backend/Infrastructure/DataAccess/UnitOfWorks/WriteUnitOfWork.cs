using Application.Interfaces.Caching;
using Application.Interfaces.DataAccess;
using Domain.Constants;
using Domain.Helpers;
using Domain.IEntities;
using Infrastructure.DataAccess.DbContexts;
using Infrastructure.DataAccess.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.DataAccess.UnitOfWorks;

public class WriteUnitOfWork : IWriteUnitOfWork
{
    private readonly DbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICacheManager _cacheManager;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<Type, object> _repositories;

    public WriteUnitOfWork(
        WriteDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ICacheManager cacheManager,
        IConfiguration configuration)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _repositories = new Dictionary<Type, object>();
        DbContext = _context;
        _httpContextAccessor = httpContextAccessor;
        _cacheManager = cacheManager;
        _configuration = configuration;
    }

    public DbContext DbContext { get; }

    public IBaseWriteRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class
    {
        var type = typeof(BaseWriteRepository<TEntity>);

        if (!_repositories.TryGetValue(type, out var value))
        {
            value = new BaseWriteRepository<TEntity>(_context);
            _repositories[type] = value;
        }

        return (IBaseWriteRepository<TEntity>)value;
    }

    public async Task<int> SaveChanges()
    {
        SaveChangesInternal();
        var result = await _context.SaveChangesAsync();

        if (result == 0) return result;

        var userId = _httpContextAccessor.HttpContext?.User.Claims.GetUserIdNullable();
        if (userId == null || userId == Guid.Empty) return result;

        var readDelayMs = _configuration.GetSection(ConfigKeys.Replication.ReadDelay).Get<int>();
        await _cacheManager.SetData(
            key: string.Format(CacheKeys.ReadAfterWrite.UserWrote, userId),
            data: true,
            duration: TimeSpan.FromMilliseconds(readDelayMs));

        return result;
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SaveChangesInternal()
    {
        var entries = _context.ChangeTracker.Entries()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();
        if (entries.Length == 0) return;

        SaveChangesInternal(entries, EntityState.Added);
        SaveChangesInternal(entries, EntityState.Modified);

        var deletedEntries = _context.ChangeTracker.Entries()
            .Where(x => x.State == EntityState.Deleted);
        SaveChangesSoftDelete(deletedEntries);
    }

    private void SaveChangesInternal(EntityEntry[] entries, EntityState state)
    {
        foreach (var item in entries)
            foreach (var p in item.Properties)
            {
                if (p.CurrentValue == null) continue;

                switch (p.Metadata.ClrType.Name)
                {
                    case "String":
                        var emptyString = string.IsNullOrWhiteSpace(p.CurrentValue.ToString());
                        p.CurrentValue = emptyString ? null : p.CurrentValue;
                        break;
                }
            }

        foreach (var item in entries.Where(t => t.State == state))
        {
            var now = DateTimeHelper.GetDtOffsetUtc();
            PropertyEntry? propertyEntry;
            if (state == EntityState.Added)
            {
                propertyEntry = item.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedBy");
                if (propertyEntry != null)
                    if (_httpContextAccessor?.HttpContext?.User != null
                        && (Guid?)propertyEntry.CurrentValue == Guid.Empty)
                        propertyEntry.CurrentValue = _httpContextAccessor.HttpContext.User.Claims.GetUserIdNullable();

                propertyEntry = item.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (propertyEntry != null) propertyEntry.CurrentValue = now;
            }

            propertyEntry = item.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedBy");
            if (propertyEntry != null)
                if (_httpContextAccessor?.HttpContext?.User != null
                    && (Guid?)propertyEntry.CurrentValue == Guid.Empty)
                    propertyEntry.CurrentValue = _httpContextAccessor.HttpContext.User.Claims.GetUserIdNullable();

            propertyEntry = item.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
            if (propertyEntry != null) propertyEntry.CurrentValue = now;

            var propertyValues = item.Properties
                .Where(p => p.CurrentValue is string && !string.IsNullOrEmpty(Convert.ToString(p.CurrentValue)));
            foreach (var propertyValue in propertyValues)
                propertyValue.CurrentValue = (propertyValue.CurrentValue?.ToString() ?? string.Empty).Trim();
        }
    }

    private void SaveChangesSoftDelete(IEnumerable<EntityEntry> entries)
    {
        foreach (var item in entries)
        {
            if (item.Entity is not IIsDeletedEntity entity) continue;

            // Unchanged (not Modified) so only the IsDeleted flag below is sent to the DB, not every field.
            item.State = EntityState.Unchanged;

            entity.IsDeleted = true;

            if (item.Entity is not IBaseEntity baseEntity) continue;

            if (_httpContextAccessor?.HttpContext?.User != null)
                baseEntity.UpdatedBy = _httpContextAccessor.HttpContext.User.Claims.GetUserIdNullable();

            baseEntity.UpdatedAt = DateTimeHelper.GetDtOffsetUtc();
        }
    }
}