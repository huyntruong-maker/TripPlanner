using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.BackgroundHandler.DbContext;

public class QuartzContext : Microsoft.EntityFrameworkCore.DbContext
{
    public QuartzContext()
    {
    }

    public QuartzContext(DbContextOptions<QuartzContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddQuartz(builder => builder.UsePostgreSql());
    }
}
