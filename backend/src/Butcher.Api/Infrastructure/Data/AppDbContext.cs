using Butcher.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductionBatch> ProductionBatches => Set<ProductionBatch>();

    public DbSet<StockUnit> StockUnits => Set<StockUnit>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
