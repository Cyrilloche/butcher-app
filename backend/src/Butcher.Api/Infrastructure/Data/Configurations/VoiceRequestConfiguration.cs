using Butcher.Api.Common;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Butcher.Api.Infrastructure.Data.Configurations;

public class VoiceRequestConfiguration : IEntityTypeConfiguration<VoiceRequest>
{
    public void Configure(EntityTypeBuilder<VoiceRequest> builder)
    {
        builder.ToTable("voice_request", table =>
            table.HasCheckConstraint("ck_voice_request_duration_ms", "duration_ms >= 0"));

        builder.HasKey(r => r.Id);

        builder.Property(r => r.OccurredAt).IsRequired();

        builder.Property(r => r.InputMode)
            .HasConversion(
                v => EnumSnakeCaseConverter.ToSnakeCase(v),
                v => EnumSnakeCaseConverter.FromSnakeCase<VoiceInputMode>(v))
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(r => r.Outcome)
            .HasConversion(
                v => EnumSnakeCaseConverter.ToSnakeCase(v),
                v => EnumSnakeCaseConverter.FromSnakeCase<VoiceRequestOutcome>(v))
            .HasMaxLength(20)
            .IsRequired();

        // Limite par compte sur l'heure glissante, usage par compte et par période (research R-04, R-08).
        builder.HasIndex(r => new { r.AccountId, r.OccurredAt });

        // Un compte n'est jamais supprimé : Restrict protège le suivi de l'usage.
        builder
            .HasOne(r => r.Account)
            .WithMany()
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
