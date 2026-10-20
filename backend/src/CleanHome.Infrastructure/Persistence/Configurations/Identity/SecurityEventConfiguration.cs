using CleanHome.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanHome.Infrastructure.Persistence.Configurations.Identity;

public sealed class SecurityEventConfiguration
    : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("security_events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id");

        builder.Property(x => x.SessionId)
            .HasColumnName("session_id");

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.Outcome)
            .HasColumnName("outcome")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Conservamos la auditoría aunque se elimine
        // posteriormente una cuenta o sesión.
        // Los identificadores se guardan como referencias
        // históricas, sin eliminación en cascada.
        builder.HasIndex(x => new
        {
            x.UserId,
            x.OccurredAt
        });

        builder.HasIndex(x => new
        {
            x.SessionId,
            x.OccurredAt
        });

        builder.HasIndex(x => new
        {
            x.EventType,
            x.OccurredAt
        });
    }
}
