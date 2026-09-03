using Butcher.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Butcher.Api.Infrastructure.Data.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movement");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasConversion<string>();

        builder.Property(m => m.SoldWeight).HasPrecision(10, 3);
        builder.Property(m => m.Amount).HasPrecision(10, 2);

        builder.HasIndex(m => m.Date);

        builder
            .HasOne(m => m.StockUnit)
            .WithMany(u => u.StockMovements)
            .HasForeignKey(m => m.StockUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(m => m.Customer)
            .WithMany(c => c.StockMovements)
            .HasForeignKey(m => m.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(m => m.CreatedBy)
            .WithMany()
            .HasForeignKey(m => m.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
