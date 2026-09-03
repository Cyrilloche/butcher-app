using Butcher.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Butcher.Api.Infrastructure.Data.Configurations;

public class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("unit_of_measure");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Label).IsRequired();
        builder.HasIndex(u => u.Label).IsUnique();

        builder.Property(u => u.Abbreviation).IsRequired();
        builder.HasIndex(u => u.Abbreviation).IsUnique();
    }
}
