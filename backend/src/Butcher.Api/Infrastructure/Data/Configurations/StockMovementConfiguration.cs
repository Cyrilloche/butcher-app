using Butcher.Api.Common;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Butcher.Api.Infrastructure.Data.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movement");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasConversion(
            v => EnumSnakeCaseConverter.ToSnakeCase(v),
            v => EnumSnakeCaseConverter.FromSnakeCase<MovementType>(v));

        builder.Property(m => m.SoldWeight).HasPrecision(10, 3);
        builder.Property(m => m.Amount).HasPrecision(10, 2);

        builder.HasIndex(m => m.Date);

        builder
            .HasOne(m => m.StockUnit)
            .WithMany(u => u.StockMovements)
            .HasForeignKey(m => m.StockUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict : la suppression d'une vente passe par SaleService, qui rétablit au passage le
        // statut des unités physiques concernées — une cascade SQL les laisserait "sold" à tort.
        builder
            .HasOne(m => m.Sale)
            .WithMany(s => s.StockMovements)
            .HasForeignKey(m => m.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(m => m.CreatedBy)
            .WithMany()
            .HasForeignKey(m => m.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
