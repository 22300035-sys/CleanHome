using CleanHome.Domain.Entities;
using CleanHome.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Infrastructure.Persistence;

public sealed class CleanHomeDbContext : DbContext
{
    public CleanHomeDbContext(
        DbContextOptions<CleanHomeDbContext> options)
        : base(options)
    {
    }

    public DbSet<ServiceCategory> ServiceCategories =>
        Set<ServiceCategory>();

    public DbSet<User> Users =>
        Set<User>();

    public DbSet<UserCredential> UserCredentials =>
        Set<UserCredential>();

    public DbSet<UserSession> UserSessions =>
        Set<UserSession>();

    public DbSet<RefreshToken> RefreshTokens =>
        Set<RefreshToken>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CleanHomeDbContext).Assembly);
    }
}
