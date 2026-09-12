using Butcher.Api.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityUserContext<AppUser, Guid>(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductionBatch> ProductionBatches => Set<ProductionBatch>();

    public DbSet<StockUnit> StockUnits => Set<StockUnit>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<UnitNumberSequence> UnitNumberSequences => Set<UnitNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // IdentityUserContext nomme ces 3 tables en PascalCase ("AspNetUserClaims"...) même avec la
        // convention de nommage snake_case active (EFCore.NamingConventions ne retouche pas un nom de
        // table déjà fixé explicitement par Identity) — on les renomme pour rester cohérent avec le
        // reste du schéma (CLAUDE.md §6).
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("app_user_claim");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("app_user_login");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("app_user_token");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    // Les deux autres surcharges de SaveChanges délèguent à celles-ci : tout enregistrement y passe.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampAuditDates();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampAuditDates();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Renseigne <c>created_at</c> à l'insertion et <c>updated_at</c> à la modification, sur toute
    /// entité qui porte ces colonnes.
    /// </summary>
    /// <remarks>
    /// En un seul endroit plutôt que dans chaque service : aucun ne le faisait, et toutes les lignes
    /// métier finissaient à <c>-infinity</c> (valeur par défaut d'un <see cref="DateTimeOffset"/>).
    /// Rien ne lit encore ces dates ; elles préparent la journalisation V2 (RF-27), dont l'historique
    /// ne se reconstruit pas après coup. Une date de création déjà posée par l'appelant est respectée.
    /// </remarks>
    private void StampAuditDates()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added
                && entry.Metadata.FindProperty("CreatedAt") is not null
                && entry.Property("CreatedAt").CurrentValue is DateTimeOffset createdAt
                && createdAt == default)
            {
                entry.Property("CreatedAt").CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified && entry.Metadata.FindProperty("UpdatedAt") is not null)
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }
}
