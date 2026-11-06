using CleanHome.Domain.Entities;
using CleanHome.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanHome.Infrastructure.Persistence.Configurations;

public sealed class MarketplaceConfiguration : IEntityTypeConfiguration<UserProfile>,
    IEntityTypeConfiguration<CustomerLocation>, IEntityTypeConfiguration<ServiceRequest>,
    IEntityTypeConfiguration<ServiceQuote>, IEntityTypeConfiguration<ServiceBooking>,
    IEntityTypeConfiguration<ChatMessage>, IEntityTypeConfiguration<PrivateDocument>,
    IEntityTypeConfiguration<AuditEntry>, IEntityTypeConfiguration<Payment>, IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notifications");
        b.Property(x => x.Kind).HasMaxLength(100);
        b.HasIndex(x => new { x.RecipientId, x.CreatedAt });
        b.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<UserProfile> b)
    {
        b.ToTable("user_profiles");
        b.HasIndex(x => x.UserId).IsUnique();
        b.Property(x => x.UserId).HasMaxLength(450);
        b.Property(x => x.DisplayName).HasMaxLength(120);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasOne<ApplicationUser>().WithOne().HasForeignKey<UserProfile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<CustomerLocation> b)
    {
        b.ToTable("customer_locations");
        b.Property(x => x.Label).HasMaxLength(80);
        b.Property(x => x.Address).HasMaxLength(500);
        b.Property(x => x.City).HasMaxLength(120);
        b.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<ServiceRequest> b)
    {
        b.ToTable("service_requests");
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.Status, x.StartsAt });
        b.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CustomerLocation>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ServiceCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<ServiceQuote> b)
    {
        b.ToTable("service_quotes", t => t.HasCheckConstraint("CK_quote_amount", "\"Amount\" > 0"));
        b.HasIndex(x => new { x.RequestId, x.WorkerId }).IsUnique();
        b.Property(x => x.Amount).HasPrecision(12, 2);
        b.Property(x => x.Currency).HasMaxLength(3);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.HasOne<ServiceRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.WorkerId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<ServiceBooking> b)
    {
        b.ToTable("service_bookings");
        b.HasIndex(x => x.RequestId).IsUnique();
        b.HasIndex(x => x.QuoteId).IsUnique();
        b.HasIndex(x => new { x.WorkerId, x.StartsAt, x.EndsAt });
        b.Property(x => x.Amount).HasPrecision(12, 2);
        b.Property(x => x.Currency).HasMaxLength(3);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasOne<ServiceRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ServiceQuote>().WithMany().HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.WorkerId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<ChatMessage> b)
    {
        b.ToTable("chat_messages");
        b.Property(x => x.Text).HasMaxLength(2000);
        b.HasIndex(x => new { x.BookingId, x.CreatedAt, x.Id });
        b.HasOne<ServiceBooking>().WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<PrivateDocument> b)
    {
        b.ToTable("private_documents");
        b.Property(x => x.StorageKey).HasMaxLength(64);
        b.Property(x => x.OriginalName).HasMaxLength(200);
        b.Property(x => x.ContentType).HasMaxLength(100);
        b.Property(x => x.ReviewNotes).HasMaxLength(1000);
        b.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<AuditEntry> b)
    {
        b.ToTable("audit_entries");
        b.Property(x => x.ActorUserId).HasMaxLength(450);
        b.Property(x => x.Action).HasMaxLength(100);
        b.HasIndex(x => x.CreatedAt);
    }
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("payments");
        // Un solo checkout por reserva: evita cobrar en dos proveedores a la vez.
        b.HasIndex(x => x.BookingId).IsUnique();
        b.Property(x => x.Provider).HasMaxLength(20);
        b.Property(x => x.CheckoutId).HasMaxLength(200);
        b.Property(x => x.ProviderPaymentId).HasMaxLength(200);
        b.Property(x => x.CheckoutUrl).HasMaxLength(2048);
        b.Property(x => x.Amount).HasPrecision(12, 2);
        b.Property(x => x.Currency).HasMaxLength(3);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasOne<ServiceBooking>().WithOne().HasForeignKey<Payment>(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
    }
}
