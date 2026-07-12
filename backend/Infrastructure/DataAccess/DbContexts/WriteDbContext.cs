using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess.DbContexts;

public sealed class WriteDbContext : BaseDbContext<WriteDbContext>
{
    public WriteDbContext()
    {
    }

    public WriteDbContext(DbContextOptions<WriteDbContext> options)
        : base(options)
    {
    }
}