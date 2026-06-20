using Domain.Entities;
using Infrastructure.DataAccess.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess.DbContexts;

public class
    BaseDbContext<TContext> : IdentityDbContext<User, Role, Guid, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
    where TContext : DbContext
{
    public BaseDbContext()
    {
    }

    public BaseDbContext(DbContextOptions<TContext> options)
        : base(options)
    {
    }

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public DbSet<Trip> Trips => Set<Trip>();

    public DbSet<ItineraryDay> ItineraryDays => Set<ItineraryDay>();

    public DbSet<TripDestination> TripDestinations => Set<TripDestination>();

    public DbSet<DestinationCache> DestinationCache => Set<DestinationCache>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Must call base first so Identity (v10+) can configure its own entities
        // (including IdentityPasskeyData added in .NET 10).
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new RoleConfiguration());
        builder.ApplyConfiguration(new RoleClaimConfiguration());
        builder.ApplyConfiguration(new UserConfiguration());
        builder.ApplyConfiguration(new UserRoleConfiguration());
        builder.ApplyConfiguration(new UserTokenConfiguration());
        builder.ApplyConfiguration(new UserLoginConfiguration());
        builder.ApplyConfiguration(new UserClaimConfiguration());
        builder.ApplyConfiguration(new EmailVerificationTokenConfiguration());
        builder.ApplyConfiguration(new TripConfiguration());
        builder.ApplyConfiguration(new ItineraryDayConfiguration());
        builder.ApplyConfiguration(new TripDestinationConfiguration());
        builder.ApplyConfiguration(new DestinationCacheConfiguration());
    }
}