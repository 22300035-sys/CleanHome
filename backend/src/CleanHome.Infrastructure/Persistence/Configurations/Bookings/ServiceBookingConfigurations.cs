using CleanHome.Domain.Entities.Bookings;
using CleanHome.Domain.Entities.Identity;
using CleanHome.Domain.Entities.Marketplace;
using CleanHome.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanHome.Infrastructure.Persistence.Configurations.Bookings;

public sealed class ServiceRequestConfiguration
    : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> b)
    {
        b.ToTable("service_requests");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.CustomerUserId)
            .HasColumnName("customer_user_id").IsRequired();

        b.Property(x => x.ServiceCategoryId)
            .HasColumnName("service_category_id").IsRequired();

        b.Property(x => x.Title)
            .HasColumnName("title").HasMaxLength(150).IsRequired();

        b.Property(x => x.Description)
            .HasColumnName("description").HasMaxLength(3000).IsRequired();

        b.Property(x => x.City)
            .HasColumnName("city").HasMaxLength(120).IsRequired();

        b.Property(x => x.ScheduledStart)
            .HasColumnName("scheduled_start")
            .HasColumnType("timestamp with time zone").IsRequired();

        b.Property(x => x.EstimatedDurationMinutes)
            .HasColumnName("estimated_duration_minutes");

        b.Property(x => x.Status)
            .HasColumnName("status").HasMaxLength(20).IsRequired();

        b.Property(x => x.CopiedFromRequestId)
            .HasColumnName("copied_from_request_id");

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        b.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<User>().WithMany()
            .HasForeignKey(x => x.CustomerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<ServiceCategory>().WithMany()
            .HasForeignKey(x => x.ServiceCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<ServiceRequest>().WithMany()
            .HasForeignKey(x => x.CopiedFromRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.CustomerUserId, x.CreatedAt });
        b.HasIndex(x => new { x.Status, x.City, x.ScheduledStart });
    }
}

public sealed class ServiceRequestAddressConfiguration
    : IEntityTypeConfiguration<ServiceRequestAddress>
{
    public void Configure(EntityTypeBuilder<ServiceRequestAddress> b)
    {
        b.ToTable("service_request_addresses");
        b.HasKey(x => x.ServiceRequestId);

        b.Property(x => x.ServiceRequestId)
            .HasColumnName("service_request_id")
            .ValueGeneratedNever();

        b.Property(x => x.Street)
            .HasColumnName("street").HasMaxLength(200).IsRequired();

        b.Property(x => x.ExteriorNumber)
            .HasColumnName("exterior_number")
            .HasMaxLength(30).IsRequired();

        b.Property(x => x.InteriorNumber)
            .HasColumnName("interior_number").HasMaxLength(30);

        b.Property(x => x.Neighborhood)
            .HasColumnName("neighborhood")
            .HasMaxLength(150).IsRequired();

        b.Property(x => x.PostalCode)
            .HasColumnName("postal_code")
            .HasMaxLength(5).IsRequired();

        b.Property(x => x.State)
            .HasColumnName("state").HasMaxLength(100).IsRequired();

        b.Property(x => x.Reference)
            .HasColumnName("reference").HasMaxLength(500);

        b.HasOne<ServiceRequest>().WithOne()
            .HasForeignKey<ServiceRequestAddress>(
                x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ServiceQuoteConfiguration
    : IEntityTypeConfiguration<ServiceQuote>
{
    public void Configure(EntityTypeBuilder<ServiceQuote> b)
    {
        b.ToTable("service_quotes");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.ServiceRequestId)
            .HasColumnName("service_request_id");

        b.Property(x => x.WorkerProfileId)
            .HasColumnName("worker_profile_id");

        b.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(12,2)");

        b.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(1500).IsRequired();

        b.Property(x => x.Status)
            .HasColumnName("status").HasMaxLength(20).IsRequired();

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        b.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<ServiceRequest>().WithMany()
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<WorkerProfile>().WithMany()
            .HasForeignKey(x => x.WorkerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new
        {
            x.ServiceRequestId,
            x.WorkerProfileId
        }).IsUnique();

        b.HasIndex(x => new { x.WorkerProfileId, x.Status });

        // Impide más de una cotización aceptada por solicitud.
        b.HasIndex(x => x.ServiceRequestId)
            .IsUnique()
            .HasFilter("status = 'accepted'");
    }
}

public sealed class ServiceBookingConfiguration
    : IEntityTypeConfiguration<ServiceBooking>
{
    public void Configure(EntityTypeBuilder<ServiceBooking> b)
    {
        b.ToTable("service_bookings");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.ServiceRequestId)
            .HasColumnName("service_request_id");

        b.Property(x => x.ServiceQuoteId)
            .HasColumnName("service_quote_id");

        b.Property(x => x.CustomerUserId)
            .HasColumnName("customer_user_id");

        b.Property(x => x.WorkerProfileId)
            .HasColumnName("worker_profile_id");

        b.Property(x => x.AgreedAmount)
            .HasColumnName("agreed_amount")
            .HasColumnType("numeric(12,2)");

        b.Property(x => x.ScheduledStart)
            .HasColumnName("scheduled_start")
            .HasColumnType("timestamp with time zone");

        b.Property(x => x.EstimatedDurationMinutes)
            .HasColumnName("estimated_duration_minutes");

        b.Property(x => x.Status)
            .HasColumnName("status").HasMaxLength(20).IsRequired();

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        b.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<ServiceRequest>().WithMany()
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<ServiceQuote>().WithOne()
            .HasForeignKey<ServiceBooking>(x => x.ServiceQuoteId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<User>().WithMany()
            .HasForeignKey(x => x.CustomerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<WorkerProfile>().WithMany()
            .HasForeignKey(x => x.WorkerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.ServiceRequestId).IsUnique();
        b.HasIndex(x => new { x.CustomerUserId, x.CreatedAt });
        b.HasIndex(x => new
        {
            x.WorkerProfileId,
            x.ScheduledStart
        });
    }
}

public sealed class BookingStatusHistoryConfiguration
    : IEntityTypeConfiguration<BookingStatusHistory>
{
    public void Configure(EntityTypeBuilder<BookingStatusHistory> b)
    {
        b.ToTable("booking_status_history");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.BookingId)
            .HasColumnName("booking_id");

        b.Property(x => x.ActorUserId)
            .HasColumnName("actor_user_id");

        b.Property(x => x.PreviousStatus)
            .HasColumnName("previous_status")
            .HasMaxLength(20).IsRequired();

        b.Property(x => x.NewStatus)
            .HasColumnName("new_status")
            .HasMaxLength(20).IsRequired();

        b.Property(x => x.Reason)
            .HasColumnName("reason").HasMaxLength(500);

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<ServiceBooking>().WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<User>().WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.BookingId, x.CreatedAt });
    }
}
