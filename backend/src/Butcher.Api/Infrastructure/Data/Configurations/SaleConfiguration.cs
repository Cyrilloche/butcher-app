using Butcher.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Butcher.Api.Infrastructure.Data.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sale");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SaleNumber).IsRequired();
        builder.HasIndex(s => s.SaleNumber).IsUnique();

        builder.HasIndex(s => s.Date);

        // Restrict : supprimer un client effacerait l'historique de traçabilité "quel lot vendu à
        // quel client" (RF-24 / OBJ-3). Un client qui a des ventes ne peut plus être supprimé.
        builder
            .HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(s => s.CreatedBy)
            .WithMany()
            .HasForeignKey(s => s.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
