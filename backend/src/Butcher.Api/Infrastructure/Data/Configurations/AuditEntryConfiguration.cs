using Butcher.Api.Common;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Butcher.Api.Infrastructure.Data.Configurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entry");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OccurredAt).IsRequired();

        builder.Property(e => e.Action)
            .HasConversion(
                v => EnumSnakeCaseConverter.ToSnakeCase(v),
                v => EnumSnakeCaseConverter.FromSnakeCase<AuditAction>(v))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.EntityType)
            .HasConversion(
                v => v == null ? null : EnumSnakeCaseConverter.ToSnakeCase(v.Value),
                v => v == null ? null : EnumSnakeCaseConverter.FromSnakeCase<AuditEntityType>(v))
            .HasMaxLength(30);

        builder.Property(e => e.EntityId).HasMaxLength(64);
        builder.Property(e => e.EntityLabel).HasMaxLength(200);
        builder.Property(e => e.DeletedContent).HasColumnType("jsonb");

        // Consultation du plus récent au plus ancien, filtrée par auteur (FR-023).
        builder.HasIndex(e => e.OccurredAt).IsDescending();
        builder.HasIndex(e => new { e.AccountId, e.OccurredAt });

        // Un compte n'est jamais supprimé (FR-009) : Restrict protège la traçabilité.
        builder
            .HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
