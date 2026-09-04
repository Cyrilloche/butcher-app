using Butcher.Api.Common;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Butcher.Api.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("product");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Name).IsRequired();

        builder.Property(p => p.SaleMode).HasConversion(
            v => EnumSnakeCaseConverter.ToSnakeCase(v),
            v => EnumSnakeCaseConverter.FromSnakeCase<SaleMode>(v));
    }
}
