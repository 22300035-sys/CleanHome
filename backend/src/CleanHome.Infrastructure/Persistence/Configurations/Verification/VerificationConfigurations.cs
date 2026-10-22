using CleanHome.Domain.Entities.Identity;
using CleanHome.Domain.Entities.Community;
using CleanHome.Domain.Entities.Verification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanHome.Infrastructure.Persistence.Configurations.Verification;

public sealed class CustomerLocationConfiguration
    : IEntityTypeConfiguration<CustomerLocation>
{
    public void Configure(EntityTypeBuilder<CustomerLocation> b)
    {
        b.ToTable("customer_locations");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.CustomerUserId)
            .HasColumnName("customer_user_id");

        b.Property(x => x.Label)
            .HasColumnName("label").HasMaxLength(80).IsRequired();

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

        b.Property(x => x.City)
            .HasColumnName("city").HasMaxLength(120).IsRequired();

        b.Property(x => x.State)
            .HasColumnName("state").HasMaxLength(100).IsRequired();

        b.Property(x => x.Reference)
            .HasColumnName("reference").HasMaxLength(500);

        b.Property(x => x.VerificationStatus)
            .HasColumnName("verification_status")
            .HasMaxLength(20).IsRequired();

        b.Property(x => x.IsActive)
            .HasColumnName("is_active");

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        b.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<User>().WithMany()
            .HasForeignKey(x => x.CustomerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.CustomerUserId);
        b.HasIndex(x => new
        {
            x.CustomerUserId,
            x.VerificationStatus
        });
    }
}

public sealed class PrivateDocumentConfiguration
    : IEntityTypeConfiguration<PrivateDocument>
{
    public void Configure(EntityTypeBuilder<PrivateDocument> b)
    {
        b.ToTable("private_documents");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.OwnerUserId)
            .HasColumnName("owner_user_id");

        b.Property(x => x.DocumentType)
            .HasColumnName("document_type")
            .HasMaxLength(50).IsRequired();

        b.Property(x => x.CustomerLocationId)
            .HasColumnName("customer_location_id");

        b.Property(x => x.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(300).IsRequired();

        b.Property(x => x.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(80).IsRequired();

        b.Property(x => x.ByteLength)
            .HasColumnName("byte_length");

        b.Property(x => x.Sha256)
            .HasColumnName("sha256")
            .HasMaxLength(64).IsRequired();

        b.Property(x => x.SecurityStatus)
            .HasColumnName("security_status")
            .HasMaxLength(20).IsRequired();

        b.Property(x => x.ModerationStatus)
            .HasColumnName("moderation_status")
            .HasMaxLength(20).IsRequired();

        b.Property(x => x.ReviewStatus)
            .HasColumnName("review_status")
            .HasMaxLength(20).IsRequired();

        b.Property(x => x.UploadedAt)
            .HasColumnName("uploaded_at")
            .HasColumnType("timestamp with time zone");

        b.Property(x => x.ReviewedAt)
            .HasColumnName("reviewed_at")
            .HasColumnType("timestamp with time zone");

        b.Property(x => x.ReviewedByUserId)
            .HasColumnName("reviewed_by_user_id");

        b.Ignore(x => x.IsApproved);

        b.HasOne<User>().WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<CustomerLocation>().WithMany()
            .HasForeignKey(x => x.CustomerLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<User>().WithMany()
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.StorageKey).IsUnique();

        b.HasIndex(x => new
        {
            x.OwnerUserId,
            x.DocumentType
        });

        b.HasIndex(x => x.CustomerLocationId);
    }
}

public sealed class DocumentAccessAuditConfiguration
    : IEntityTypeConfiguration<DocumentAccessAudit>
{
    public void Configure(EntityTypeBuilder<DocumentAccessAudit> b)
    {
        b.ToTable("document_access_audits");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id").ValueGeneratedNever();

        b.Property(x => x.DocumentId)
            .HasColumnName("document_id");

        b.Property(x => x.ActorUserId)
            .HasColumnName("actor_user_id");

        b.Property(x => x.AccessRole)
            .HasColumnName("access_role")
            .HasMaxLength(30).IsRequired();

        b.Property(x => x.Action)
            .HasColumnName("action")
            .HasMaxLength(30).IsRequired();

        b.Property(x => x.SupportIncidentId)
            .HasColumnName("support_incident_id");

        b.Property(x => x.AccessedAt)
            .HasColumnName("accessed_at")
            .HasColumnType("timestamp with time zone");

        b.HasOne<PrivateDocument>().WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<User>().WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<SupportIncident>().WithMany()
            .HasForeignKey(x => x.SupportIncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new
        {
            x.DocumentId,
            x.AccessedAt
        });

        b.HasIndex(x => new
        {
            x.ActorUserId,
            x.AccessedAt
        });
    }
}
