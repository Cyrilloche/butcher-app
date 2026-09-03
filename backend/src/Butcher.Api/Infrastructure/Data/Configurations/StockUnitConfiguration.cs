using Butcher.Api.Common;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Butcher.Api.Infrastructure.Data.Configurations;

public class StockUnitConfiguration : IEntityTypeConfiguration<StockUnit>
{
    public void Configure(EntityTypeBuilder<StockUnit> builder)
    {
        builder.ToTable("stock_unit");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Weight).HasPrecision(10, 3);

        builder.Property(u => u.Status).HasConversion(
            v => EnumSnakeCaseConverter.ToSnakeCase(v),
            v => EnumSnakeCaseConverter.FromSnakeCase<StockUnitStatus>(v));
        builder.HasIndex(u => u.Status);

        builder
            .HasOne(u => u.Batch)
            .WithMany(b => b.StockUnits)
            .HasForeignKey(u => u.BatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
