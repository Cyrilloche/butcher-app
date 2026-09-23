using Butcher.Api.Common;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Butcher.Api.Infrastructure.Data.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("app_user");

        builder.Property(u => u.CreatedAt).IsRequired();

        builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();

        // Pas de valeur par défaut côté modèle : EF prendrait la valeur CLR par défaut de l'enum pour
        // « non renseignée » et laisserait la base décider. Le rôle est toujours écrit explicitement.
        builder.Property(u => u.Role)
            .HasConversion(
                v => EnumSnakeCaseConverter.ToSnakeCase(v),
                v => EnumSnakeCaseConverter.FromSnakeCase<AccountRole>(v))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.IsActive).IsRequired();

        // Seule valeur par défaut en base de la table : la migration l'exige pour les comptes existants,
        // qui n'ont pas l'assistant tant que l'administrateur ne l'active pas (RF-36).
        builder.Property(u => u.AssistantEnabled).HasDefaultValue(false).IsRequired();

        // Identity utilise NormalizedEmail (pas Email) pour ses recherches/comparaisons ;
        // c'est donc lui, pas Email, qui porte la contrainte d'unicité en base.
        builder.HasIndex(u => u.NormalizedEmail).IsUnique();
    }
}
