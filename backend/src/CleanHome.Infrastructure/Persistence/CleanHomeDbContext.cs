using CleanHome.Domain.Entities;
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

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CleanHomeDbContext).Assembly);
    }
}
