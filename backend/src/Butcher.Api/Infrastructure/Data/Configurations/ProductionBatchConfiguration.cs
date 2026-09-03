using Butcher.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Butcher.Api.Infrastructure.Data.Configurations;

public class ProductionBatchConfiguration : IEntityTypeConfiguration<ProductionBatch>
{
    public void Configure(EntityTypeBuilder<ProductionBatch> builder)
    {
        builder.ToTable("production_batch");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BatchNumber).IsRequired();
        builder.HasIndex(b => b.BatchNumber).IsUnique();

        builder.Property(b => b.SalePrice).HasPrecision(10, 2);

        builder
            .HasOne(b => b.Product)
            .WithMany(p => p.ProductionBatches)
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(b => b.CreatedBy)
            .WithMany()
            .HasForeignKey(b => b.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
