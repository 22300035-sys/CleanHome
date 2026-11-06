using CleanHome.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using CleanHome.Infrastructure.Identity;

namespace CleanHome.Infrastructure.Persistence;

public sealed class CleanHomeDbContext : IdentityDbContext<ApplicationUser>
{
    public CleanHomeDbContext(
        DbContextOptions<CleanHomeDbContext> options)
        : base(options)
    {
    }

    public DbSet<ServiceCategory> ServiceCategories =>
        Set<ServiceCategory>();
    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<CustomerLocation> Locations => Set<CustomerLocation>();
    public DbSet<ServiceRequest> Requests => Set<ServiceRequest>();
    public DbSet<ServiceQuote> Quotes => Set<ServiceQuote>();
    public DbSet<ServiceBooking> Bookings => Set<ServiceBooking>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();
    public DbSet<PrivateDocument> Documents => Set<PrivateDocument>();
    public DbSet<AuditEntry> Audit => Set<AuditEntry>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<UserProfile>().Where(x => x.State == EntityState.Modified))
            entry.Entity.Version = Guid.NewGuid();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CleanHomeDbContext).Assembly);
    }
}
