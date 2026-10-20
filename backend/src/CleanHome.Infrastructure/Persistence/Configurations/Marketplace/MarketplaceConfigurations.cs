using CleanHome.Domain.Entities.Identity;
using CleanHome.Domain.Entities.Marketplace;
using CleanHome.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanHome.Infrastructure.Persistence.Configurations.Marketplace;

public sealed class UserProfileConfiguration
    : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> b)
    {
        b.ToTable("user_profiles");
        b.HasKey(x => x.UserId);

        b.Property(x => x.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedNever();

        b.Property(x => x.City)
            .HasColumnName("city")
            .HasMaxLength(120).IsRequired();

        b.Property(x => x.Phone)
            .HasColumnName("phone")
            .HasMaxLength(25);

        b.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<User>().WithOne()
            .HasForeignKey<UserProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WorkerProfileConfiguration
    : IEntityTypeConfiguration<WorkerProfile>
{
    public void Configure(EntityTypeBuilder<WorkerProfile> b)
    {
        b.ToTable("worker_profiles");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.UserId)
            .HasColumnName("user_id").IsRequired();

        b.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(1500).IsRequired();

        b.Property(x => x.ServiceCity)
            .HasColumnName("service_city")
            .HasMaxLength(120).IsRequired();

        b.Property(x => x.ServiceArea)
            .HasColumnName("service_area")
            .HasMaxLength(200).IsRequired();

        b.Property(x => x.VerificationStatus)
            .HasColumnName("verification_status")
            .HasMaxLength(20).IsRequired();

        b.Property(x => x.IsPublished)
            .HasColumnName("is_published");

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        b.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<User>().WithOne()
            .HasForeignKey<WorkerProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.UserId).IsUnique();
        b.HasIndex(x => new { x.IsPublished, x.ServiceCity });
    }
}

public sealed class WorkerOfferingConfiguration
    : IEntityTypeConfiguration<WorkerOffering>
{
    public void Configure(EntityTypeBuilder<WorkerOffering> b)
    {
        b.ToTable("worker_offerings");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.WorkerProfileId)
            .HasColumnName("worker_profile_id");

        b.Property(x => x.ServiceCategoryId)
            .HasColumnName("service_category_id");

        b.Property(x => x.ReferencePrice)
            .HasColumnName("reference_price")
            .HasColumnType("numeric(12,2)");

        b.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(500).IsRequired();

        b.Property(x => x.IsActive)
            .HasColumnName("is_active");

        b.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<WorkerProfile>().WithMany()
            .HasForeignKey(x => x.WorkerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<ServiceCategory>().WithMany()
            .HasForeignKey(x => x.ServiceCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new
        {
            x.WorkerProfileId,
            x.ServiceCategoryId
        }).IsUnique();
    }
}

public sealed class WorkerAvailabilityConfiguration
    : IEntityTypeConfiguration<WorkerAvailability>
{
    public void Configure(EntityTypeBuilder<WorkerAvailability> b)
    {
        b.ToTable("worker_availability");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.WorkerProfileId)
            .HasColumnName("worker_profile_id");

        b.Property(x => x.DayOfWeek)
            .HasColumnName("day_of_week");

        b.Property(x => x.StartTime)
            .HasColumnName("start_time")
            .HasColumnType("time without time zone");

        b.Property(x => x.EndTime)
            .HasColumnName("end_time")
            .HasColumnType("time without time zone");

        b.HasOne<WorkerProfile>().WithMany()
            .HasForeignKey(x => x.WorkerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new
        {
            x.WorkerProfileId,
            x.DayOfWeek,
            x.StartTime,
            x.EndTime
        }).IsUnique();
    }
}

public sealed class WorkerDocumentConfiguration
    : IEntityTypeConfiguration<WorkerDocument>
{
    public void Configure(EntityTypeBuilder<WorkerDocument> b)
    {
        b.ToTable("worker_documents");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.WorkerProfileId)
            .HasColumnName("worker_profile_id");

        b.Property(x => x.DocumentType)
            .HasColumnName("document_type")
            .HasMaxLength(50).IsRequired();

        b.Property(x => x.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(300).IsRequired();

        b.Property(x => x.ReviewStatus)
            .HasColumnName("review_status")
            .HasMaxLength(20).IsRequired();

        b.Property(x => x.UploadedAt)
            .HasColumnName("uploaded_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<WorkerProfile>().WithMany()
            .HasForeignKey(x => x.WorkerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.WorkerProfileId);
    }
}
