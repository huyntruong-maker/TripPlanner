using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess.DbContexts;

public sealed class ReadDbContext : BaseDbContext<ReadDbContext>
{
    public ReadDbContext()
    {
    }

    public ReadDbContext(DbContextOptions<ReadDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }
}