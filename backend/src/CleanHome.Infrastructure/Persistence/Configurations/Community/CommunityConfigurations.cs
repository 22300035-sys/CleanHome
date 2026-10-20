using CleanHome.Domain.Entities.Bookings;
using CleanHome.Domain.Entities.Community;
using CleanHome.Domain.Entities.Identity;
using CleanHome.Domain.Entities.Marketplace;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanHome.Infrastructure.Persistence.Configurations.Community;

public sealed class ChatMessageConfiguration
    : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> b)
    {
        b.ToTable("chat_messages");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.BookingId)
            .HasColumnName("booking_id");

        b.Property(x => x.SenderUserId)
            .HasColumnName("sender_user_id");

        b.Property(x => x.Content)
            .HasColumnName("content")
            .HasMaxLength(4000).IsRequired();

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        b.Property(x => x.ReadAt)
            .HasColumnName("read_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<ServiceBooking>().WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<User>().WithMany()
            .HasForeignKey(x => x.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.BookingId, x.CreatedAt });
    }
}

public sealed class UserNotificationConfiguration
    : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> b)
    {
        b.ToTable("user_notifications");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.UserId)
            .HasColumnName("user_id");

        b.Property(x => x.Type)
            .HasColumnName("type")
            .HasMaxLength(60).IsRequired();

        b.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(150).IsRequired();

        b.Property(x => x.Body)
            .HasColumnName("body")
            .HasMaxLength(1000).IsRequired();

        b.Property(x => x.RelatedEntityId)
            .HasColumnName("related_entity_id");

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        b.Property(x => x.ReadAt)
            .HasColumnName("read_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<User>().WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.UserId, x.CreatedAt });
        b.HasIndex(x => new { x.UserId, x.ReadAt });
    }
}

public sealed class ServiceReviewConfiguration
    : IEntityTypeConfiguration<ServiceReview>
{
    public void Configure(EntityTypeBuilder<ServiceReview> b)
    {
        b.ToTable("service_reviews");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.BookingId)
            .HasColumnName("booking_id");

        b.Property(x => x.CustomerUserId)
            .HasColumnName("customer_user_id");

        b.Property(x => x.WorkerProfileId)
            .HasColumnName("worker_profile_id");

        b.Property(x => x.Rating)
            .HasColumnName("rating");

        b.Property(x => x.Comment)
            .HasColumnName("comment")
            .HasMaxLength(2000).IsRequired();

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<ServiceBooking>().WithOne()
            .HasForeignKey<ServiceReview>(x => x.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<User>().WithMany()
            .HasForeignKey(x => x.CustomerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<WorkerProfile>().WithMany()
            .HasForeignKey(x => x.WorkerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.BookingId).IsUnique();

        b.HasIndex(x => new
        {
            x.WorkerProfileId,
            x.CreatedAt
        });

        b.ToTable(t => t.HasCheckConstraint(
            "CK_service_reviews_rating",
            "rating BETWEEN 1 AND 5"));
    }
}

public sealed class SupportIncidentConfiguration
    : IEntityTypeConfiguration<SupportIncident>
{
    public void Configure(EntityTypeBuilder<SupportIncident> b)
    {
        b.ToTable("support_incidents");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.ReporterUserId)
            .HasColumnName("reporter_user_id");

        b.Property(x => x.BookingId)
            .HasColumnName("booking_id");

        b.Property(x => x.Subject)
            .HasColumnName("subject")
            .HasMaxLength(150).IsRequired();

        b.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(4000).IsRequired();

        b.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20).IsRequired();

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        b.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<User>().WithMany()
            .HasForeignKey(x => x.ReporterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<ServiceBooking>().WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.ReporterUserId, x.CreatedAt });
        b.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}

public sealed class IncidentCommentConfiguration
    : IEntityTypeConfiguration<IncidentComment>
{
    public void Configure(EntityTypeBuilder<IncidentComment> b)
    {
        b.ToTable("incident_comments");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.IncidentId)
            .HasColumnName("incident_id");

        b.Property(x => x.AuthorUserId)
            .HasColumnName("author_user_id");

        b.Property(x => x.Content)
            .HasColumnName("content")
            .HasMaxLength(3000).IsRequired();

        b.Property(x => x.IsInternal)
            .HasColumnName("is_internal");

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<SupportIncident>().WithMany()
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<User>().WithMany()
            .HasForeignKey(x => x.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.IncidentId, x.CreatedAt });
    }
}

public sealed class AdminMembershipConfiguration
    : IEntityTypeConfiguration<AdminMembership>
{
    public void Configure(EntityTypeBuilder<AdminMembership> b)
    {
        b.ToTable("admin_memberships");
        b.HasKey(x => x.UserId);

        b.Property(x => x.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedNever();

        b.Property(x => x.Role)
            .HasColumnName("role")
            .HasMaxLength(30).IsRequired();

        b.Property(x => x.IsEnabled)
            .HasColumnName("is_enabled");

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        b.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<User>().WithOne()
            .HasForeignKey<AdminMembership>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.Role, x.IsEnabled });
    }
}
