using Butcher.Api.Common.Authorization;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Infrastructure.Data.Audit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Infrastructure.Data;

/// <param name="options">Options EF Core.</param>
/// <param name="currentAccount">
/// Compte à l'origine des écritures, pour renseigner l'auteur. Facultatif : absent hors requête
/// HTTP (commandes hors ligne, migrations), où l'auteur reste inconnu.
/// </param>
public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentAccount? currentAccount = null)
    : IdentityUserContext<AppUser, Guid>(options)
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

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<VoiceRequest> VoiceRequests => Set<VoiceRequest>();

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
    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        SaveWithAuditTrailAsync(acceptAllChangesOnSuccess, useAsync: false, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        SaveWithAuditTrailAsync(acceptAllChangesOnSuccess, useAsync: true, cancellationToken);

    /// <summary>
    /// Enregistre, et écrit au journal les gestes que cet enregistrement raconte (FR-021, research R-09).
    /// </summary>
    /// <remarks>
    /// Les entrées sont préparées avant l'écriture, tant que l'état d'origine de chaque objet est lisible,
    /// et écrites juste après, quand les objets créés ont leur identifiant. Les deux écritures partagent
    /// une transaction — celle de l'appelant s'il en a ouvert une : une opération qui échoue ne laisse
    /// aucune entrée, et aucune opération n'échappe au journal.
    /// </remarks>
    private async Task<int> SaveWithAuditTrailAsync(bool acceptAllChangesOnSuccess, bool useAsync, CancellationToken cancellationToken)
    {
        StampAuditDates();
        StampAuthor();

        var pending = await AuditTrail.CollectAsync(this, currentAccount?.AccountId, cancellationToken);
        if (pending.Count == 0)
        {
            return useAsync
                ? await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken)
                : base.SaveChanges(acceptAllChangesOnSuccess);
        }

        var ownTransaction = Database.CurrentTransaction is null
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var written = useAsync
                ? await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken)
                : base.SaveChanges(acceptAllChangesOnSuccess);

            await AuditTrail.WriteAsync(this, pending, cancellationToken);

            if (ownTransaction is not null)
            {
                await ownTransaction.CommitAsync(cancellationToken);
            }

            return written;
        }
        finally
        {
            if (ownTransaction is not null)
            {
                await ownTransaction.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Renseigne <c>created_by</c> à l'insertion, sur toute entité qui porte cette colonne, à partir
    /// du compte de la requête (RF-27, ADR-011).
    /// </summary>
    /// <remarks>
    /// Même logique que <see cref="StampAuditDates"/> : un seul endroit, pour qu'aucun service n'ait à
    /// y penser et qu'aucune nouvelle entité ne l'oublie. Un auteur déjà posé par l'appelant est
    /// respecté ; sans compte courant, l'auteur reste inconnu.
    /// </remarks>
    private void StampAuthor()
    {
        if (currentAccount?.AccountId is not { } accountId)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added
                && entry.Metadata.FindProperty("CreatedById") is not null
                && entry.Property("CreatedById").CurrentValue is null)
            {
                entry.Property("CreatedById").CurrentValue = accountId;
            }
        }
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
