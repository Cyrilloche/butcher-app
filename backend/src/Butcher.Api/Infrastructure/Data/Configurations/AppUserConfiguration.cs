using Butcher.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Butcher.Api.Infrastructure.Data.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("app_user");

        builder.Property(u => u.CreatedAt).IsRequired();

        // Identity utilise NormalizedEmail (pas Email) pour ses recherches/comparaisons ;
        // c'est donc lui, pas Email, qui porte la contrainte d'unicité en base.
        builder.HasIndex(u => u.NormalizedEmail).IsUnique();
    }
}
