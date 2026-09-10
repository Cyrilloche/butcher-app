using Butcher.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Butcher.Api.Infrastructure.Data.Configurations;

public class BatchNumberSequenceConfiguration : IEntityTypeConfiguration<BatchNumberSequence>
{
    public void Configure(EntityTypeBuilder<BatchNumberSequence> builder)
    {
        builder.ToTable("batch_number_sequence");

        builder.HasKey(s => new { s.ProductId, s.ProductionDate });

        builder.Property(s => s.LastSequence).IsRequired();

        builder
            .HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
