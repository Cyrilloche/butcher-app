using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;
using NpgsqlTypes;

namespace Butcher.Api.Infrastructure.Data.Audit;

/// <summary>Entrée du journal préparée avant l'écriture, complétée juste après.</summary>
internal sealed class PendingAuditEntry
{
    public required AuditAction Action { get; init; }

    public required AuditEntityType EntityType { get; init; }

    /// <summary>Lu après l'écriture : l'identifiant d'un objet créé n'existe qu'à ce moment-là.</summary>
    public required Func<string?> EntityId { get; init; }

    public required string Label { get; init; }

    public string? DeletedContent { get; init; }

    public Guid? AccountId { get; init; }
}

/// <summary>
/// Déduit les gestes d'un enregistrement à partir du <see cref="ChangeTracker"/>, et les écrit au journal
/// (FR-021, FR-022 ; specs/005-backoffice, data-model §3.1).
/// </summary>
/// <remarks>
/// Une entrée par geste, pas une par ligne modifiée (clarification du 2026-09-13) : les lignes d'une vente
/// suivent la vente, les unités d'une fournée suivent la fournée, et une unité dont le statut change sous
/// l'effet d'un mouvement du même enregistrement n'a pas d'entrée. Seules les suppressions gardent le
/// contenu de l'objet. Les libellés sont en français et figés au moment du geste : l'objet peut disparaître
/// ou changer ensuite.
/// </remarks>
internal sealed class AuditTrail
{
    private const int LabelMaxLength = 200;

    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private static readonly JsonSerializerOptions ContentJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    private readonly AppDbContext _dbContext;
    private readonly Guid? _currentAccountId;
    private readonly List<EntityEntry> _changes;
    private readonly List<PendingAuditEntry> _pending = [];

    /// <summary>Objets déjà racontés par le geste d'un autre (lignes d'une vente, unités d'une fournée).</summary>
    private readonly HashSet<object> _handled = new(ReferenceEqualityComparer.Instance);

    private AuditTrail(AppDbContext dbContext, Guid? currentAccountId, List<EntityEntry> changes)
    {
        _dbContext = dbContext;
        _currentAccountId = currentAccountId;
        _changes = changes;
    }

    public static async Task<List<PendingAuditEntry>> CollectAsync(
        AppDbContext dbContext, Guid? currentAccountId, CancellationToken cancellationToken)
    {
        var changes = dbContext.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (changes.Count == 0)
        {
            return [];
        }

        var trail = new AuditTrail(dbContext, currentAccountId, changes);
        await trail.CollectSalesAsync(cancellationToken);
        await trail.CollectMovementsAsync(cancellationToken);
        await trail.CollectBatchesAsync(cancellationToken);
        await trail.CollectUnitsAsync(cancellationToken);
        trail.CollectProducts();
        trail.CollectCustomers();
        trail.CollectAccounts();
        return trail._pending;
    }

    /// <summary>
    /// Écrit les entrées préparées. Par SQL direct plutôt que par le <see cref="ChangeTracker"/> : l'appelant
    /// n'a peut-être pas encore accepté ses propres changements, qu'un second <c>SaveChanges</c> réécrirait.
    /// </summary>
    public static async Task WriteAsync(
        AppDbContext dbContext, IReadOnlyList<PendingAuditEntry> pending, CancellationToken cancellationToken)
    {
        var occurredAt = DateTimeOffset.UtcNow;

        foreach (var entry in pending)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO audit_entry (occurred_at, account_id, action, entity_type, entity_id, entity_label, deleted_content)
                VALUES (@occurred_at, @account_id, @action, @entity_type, @entity_id, @entity_label, @deleted_content)
                """,
                [
                    new NpgsqlParameter("occurred_at", NpgsqlDbType.TimestampTz) { Value = occurredAt },
                    new NpgsqlParameter("account_id", NpgsqlDbType.Uuid) { Value = (object?)entry.AccountId ?? DBNull.Value },
                    new NpgsqlParameter("action", NpgsqlDbType.Varchar) { Value = Snake(entry.Action) },
                    new NpgsqlParameter("entity_type", NpgsqlDbType.Varchar) { Value = Snake(entry.EntityType) },
                    new NpgsqlParameter("entity_id", NpgsqlDbType.Varchar) { Value = (object?)entry.EntityId() ?? DBNull.Value },
                    new NpgsqlParameter("entity_label", NpgsqlDbType.Varchar) { Value = entry.Label },
                    new NpgsqlParameter("deleted_content", NpgsqlDbType.Jsonb) { Value = (object?)entry.DeletedContent ?? DBNull.Value },
                ],
                cancellationToken);
        }
    }

    // --- Ventes et lignes ------------------------------------------------------------------------------

    private async Task CollectSalesAsync(CancellationToken ct)
    {
        foreach (var entry in Changes<Sale>())
        {
            var sale = (Sale)entry.Entity;
            var lines = Changes<StockMovement>()
                .Where(m => m.State == entry.State && BelongsTo((StockMovement)m.Entity, sale))
                .Select(m => (StockMovement)m.Entity)
                .ToList();

            switch (entry.State)
            {
                case EntityState.Added:
                    MarkHandled(lines);
                    Add(AuditAction.Created, AuditEntityType.Sale, () => Id(sale.Id),
                        $"{sale.SaleNumber} ({Count(lines.Count, "ligne")})");
                    break;

                case EntityState.Modified when HasMeaningfulChange(entry):
                    Add(AuditAction.Updated, AuditEntityType.Sale, () => Id(sale.Id), sale.SaleNumber);
                    break;

                case EntityState.Deleted:
                    MarkHandled(lines);
                    Add(AuditAction.Deleted, AuditEntityType.Sale, () => Id(sale.Id), sale.SaleNumber,
                        await SaleContentAsync(sale, lines, ct));
                    break;
            }
        }
    }

    private async Task CollectMovementsAsync(CancellationToken ct)
    {
        var remaining = Changes<StockMovement>().Where(e => !_handled.Contains(e.Entity)).ToList();

        // Sorties perso ou perte ajoutées ensemble (solde du stock d'un produit) : une entrée par type.
        var outcomes = remaining
            .Where(e => e.State == EntityState.Added && ((StockMovement)e.Entity).Type != MovementType.Sale)
            .ToList();

        foreach (var group in outcomes.GroupBy(e => ((StockMovement)e.Entity).Type))
        {
            var movements = group.Select(e => (StockMovement)e.Entity).ToList();
            var outcome = OutcomeLabel(group.Key);

            if (movements.Count == 1)
            {
                var movement = movements[0];
                var unit = await UnitInfoAsync(movement.StockUnitId, movement.StockUnit, ct);
                Add(AuditAction.Created, AuditEntityType.StockMovement, () => Id(movement.Id),
                    $"{outcome} · {unit.UnitNumber}");
                continue;
            }

            var productNames = new List<string>();
            foreach (var movement in movements)
            {
                var unit = await UnitInfoAsync(movement.StockUnitId, movement.StockUnit, ct);
                if (unit.ProductName is not null && !productNames.Contains(unit.ProductName))
                {
                    productNames.Add(unit.ProductName);
                }
            }

            Add(AuditAction.Created, AuditEntityType.StockMovement, () => null,
                $"{outcome} · {string.Join(", ", productNames)} ({Count(movements.Count, "unité")})");
        }

        foreach (var entry in remaining.Except(outcomes))
        {
            var movement = (StockMovement)entry.Entity;

            switch (entry.State)
            {
                case EntityState.Added:
                    Add(AuditAction.Created, AuditEntityType.StockMovement, () => Id(movement.Id),
                        await MovementLabelAsync(movement, ct));
                    break;

                case EntityState.Modified when HasMeaningfulChange(entry):
                    Add(AuditAction.Updated, AuditEntityType.StockMovement, () => Id(movement.Id),
                        await MovementLabelAsync(movement, ct));
                    break;

                case EntityState.Deleted:
                    Add(AuditAction.Deleted, AuditEntityType.StockMovement, () => Id(movement.Id),
                        await MovementLabelAsync(movement, ct), await MovementContentAsync(movement, ct));
                    break;
            }
        }
    }

    // --- Fournées et unités ----------------------------------------------------------------------------

    private async Task CollectBatchesAsync(CancellationToken ct)
    {
        foreach (var entry in Changes<ProductionBatch>())
        {
            var batch = (ProductionBatch)entry.Entity;
            var productName = await ProductNameAsync(batch.ProductId, batch.Product, ct);
            var label = $"{productName} — {FormatDate(batch.ProductionDate)}";

            switch (entry.State)
            {
                case EntityState.Added:
                {
                    var units = UnitsOf(batch, EntityState.Added);
                    MarkHandled(units);
                    Add(AuditAction.Created, AuditEntityType.ProductionBatch, () => Id(batch.Id),
                        units.Count > 0 ? $"{label} ({Count(units.Count, "unité")})" : label);
                    break;
                }

                case EntityState.Modified when HasMeaningfulChange(entry):
                    Add(AuditAction.Updated, AuditEntityType.ProductionBatch, () => Id(batch.Id), label);
                    break;

                case EntityState.Deleted:
                {
                    var units = UnitsOf(batch, EntityState.Deleted);
                    MarkHandled(units);
                    Add(AuditAction.Deleted, AuditEntityType.ProductionBatch, () => Id(batch.Id), label, new
                    {
                        ProductName = productName,
                        batch.ProductionDate,
                        batch.SalePrice,
                        batch.RawMaterialRef,
                        batch.ExpiryDate,
                        batch.Notes,
                        Units = units.OrderBy(u => u.Id).Select(u => new { u.UnitNumber, u.Weight, u.Status }),
                    });
                    break;
                }
            }
        }
    }

    private async Task CollectUnitsAsync(CancellationToken ct)
    {
        var remaining = Changes<StockUnit>().Where(e => !_handled.Contains(e.Entity)).ToList();

        // Unités pesées sur une fournée déjà enregistrée : une entrée par fournée.
        var added = remaining.Where(e => e.State == EntityState.Added).Select(e => (StockUnit)e.Entity);
        foreach (var group in added.GroupBy(u => u.BatchId))
        {
            // Le ChangeTracker ne rend pas les objets dans leur ordre d'ajout : on range par rang d'étiquette.
            var units = group
                .OrderBy(u => SequenceOf(u.UnitNumber))
                .ThenBy(u => u.UnitNumber, StringComparer.Ordinal)
                .ToList();
            var batch = await BatchInfoAsync(group.Key, units[0].Batch, ct);
            var numbers = units.Count == 1
                ? units[0].UnitNumber
                : $"{units.Count} : {units[0].UnitNumber} à {units[^1].UnitNumber}";
            var single = units.Count == 1 ? units[0] : null;

            Add(AuditAction.Created, AuditEntityType.StockUnit, () => single is null ? null : Id(single.Id),
                $"{batch.ProductName} — {FormatDate(batch.ProductionDate)} ({numbers})");
        }

        // Unités dont un mouvement change dans le même enregistrement : leur statut n'est qu'une conséquence.
        var movedUnitIds = Changes<StockMovement>().Select(e => ((StockMovement)e.Entity).StockUnitId).ToHashSet();

        foreach (var entry in remaining.Where(e => e.State != EntityState.Added))
        {
            var unit = (StockUnit)entry.Entity;

            if (entry.State == EntityState.Deleted)
            {
                var batch = await BatchInfoAsync(unit.BatchId, unit.Batch, ct);
                Add(AuditAction.Deleted, AuditEntityType.StockUnit, () => Id(unit.Id), unit.UnitNumber, new
                {
                    unit.UnitNumber,
                    batch.ProductName,
                    batch.ProductionDate,
                    unit.Weight,
                    unit.Status,
                });
                continue;
            }

            if (movedUnitIds.Contains(unit.Id) || !HasMeaningfulChange(entry))
            {
                continue;
            }

            var status = entry.Property(nameof(StockUnit.Status));
            var closed = status.OriginalValue is StockUnitStatus.Opened && unit.Status == StockUnitStatus.Sold;
            Add(AuditAction.Updated, AuditEntityType.StockUnit, () => Id(unit.Id),
                closed ? $"{unit.UnitNumber} — clôturée" : unit.UnitNumber);
        }
    }

    // --- Référentiels et comptes -----------------------------------------------------------------------

    private void CollectProducts()
    {
        foreach (var entry in Changes<Product>())
        {
            var product = (Product)entry.Entity;
            var label = $"{product.Name} ({product.Code})";

            switch (entry.State)
            {
                case EntityState.Added:
                    Add(AuditAction.Created, AuditEntityType.Product, () => Id(product.Id), label);
                    break;

                case EntityState.Modified when HasMeaningfulChange(entry):
                    if (IsChanged(entry, nameof(Product.IsActive)))
                    {
                        label += product.IsActive ? " — réactivé" : " — désactivé";
                    }

                    Add(AuditAction.Updated, AuditEntityType.Product, () => Id(product.Id), label);
                    break;

                case EntityState.Deleted:
                    Add(AuditAction.Deleted, AuditEntityType.Product, () => Id(product.Id), label, new
                    {
                        product.Code,
                        product.Name,
                        product.SaleMode,
                        product.AllowPartialSale,
                        product.IsActive,
                    });
                    break;
            }
        }
    }

    private void CollectCustomers()
    {
        foreach (var entry in Changes<Customer>())
        {
            var customer = (Customer)entry.Entity;
            var label = FormatCustomerName(customer.FirstName, customer.LastName);

            switch (entry.State)
            {
                case EntityState.Added:
                    Add(AuditAction.Created, AuditEntityType.Customer, () => Id(customer.Id), label);
                    break;

                case EntityState.Modified when HasMeaningfulChange(entry):
                    Add(AuditAction.Updated, AuditEntityType.Customer, () => Id(customer.Id), label);
                    break;

                case EntityState.Deleted:
                    Add(AuditAction.Deleted, AuditEntityType.Customer, () => Id(customer.Id), label, new
                    {
                        customer.LastName,
                        customer.FirstName,
                        customer.Phone,
                        customer.Notes,
                    });
                    break;
            }
        }
    }

    /// <summary>
    /// Comptes : création, nom, rôle, état. Le mot de passe et le verrouillage ont leur propre nature
    /// d'entrée ; la dernière connexion, le compteur d'échecs et les jetons d'Identity n'en ont aucune.
    /// </summary>
    private void CollectAccounts()
    {
        foreach (var entry in Changes<AppUser>())
        {
            var account = (AppUser)entry.Entity;
            string AccountId() => account.Id.ToString();

            if (entry.State == EntityState.Added)
            {
                Add(AuditAction.Created, AuditEntityType.Account, AccountId, $"{account.DisplayName} ({account.Email})");
                continue;
            }

            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            // Hors requête authentifiée (connexion, commande hors ligne), l'auteur est le compte lui-même.
            var author = _currentAccountId ?? account.Id;

            if (IsChanged(entry, nameof(AppUser.PasswordHash)))
            {
                Add(AuditAction.PasswordChanged, AuditEntityType.Account, AccountId, account.DisplayName, accountId: author);
            }

            if (IsChanged(entry, nameof(AppUser.LockoutEnd)) && account.LockoutEnd > DateTimeOffset.UtcNow)
            {
                Add(AuditAction.LockedOut, AuditEntityType.Account, AccountId, account.DisplayName, accountId: author);
            }

            var details = new List<string>();
            if (IsChanged(entry, nameof(AppUser.Role)))
            {
                details.Add($"rôle : {(account.Role == AccountRole.Admin ? "Administrateur" : "Utilisateur")}");
            }

            if (IsChanged(entry, nameof(AppUser.IsActive)))
            {
                details.Add(account.IsActive ? "réactivé" : "désactivé");
            }

            if (details.Count > 0 || IsChanged(entry, nameof(AppUser.DisplayName)) || IsChanged(entry, nameof(AppUser.Email)))
            {
                var label = details.Count > 0 ? $"{account.DisplayName} — {string.Join(", ", details)}" : account.DisplayName;
                Add(AuditAction.Updated, AuditEntityType.Account, AccountId, label, accountId: author);
            }
        }
    }

    // --- Contenus et libellés --------------------------------------------------------------------------

    private async Task<object> SaleContentAsync(Sale sale, List<StockMovement> lines, CancellationToken ct)
    {
        var customer = sale.Customer ?? await _dbContext.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == sale.CustomerId, ct);

        var lineContents = new List<object>();
        foreach (var line in lines.OrderBy(l => l.Id))
        {
            var unit = await UnitInfoAsync(line.StockUnitId, line.StockUnit, ct);
            lineContents.Add(new { unit.UnitNumber, unit.ProductName, line.SoldWeight, line.Amount, line.Notes });
        }

        return new
        {
            sale.SaleNumber,
            sale.Date,
            sale.CustomerId,
            CustomerName = customer is null ? null : FormatCustomerName(customer.FirstName, customer.LastName),
            sale.Paid,
            sale.Notes,
            Total = lines.Sum(l => l.Amount ?? 0m),
            Lines = lineContents,
        };
    }

    private async Task<object> MovementContentAsync(StockMovement movement, CancellationToken ct)
    {
        var unit = await UnitInfoAsync(movement.StockUnitId, movement.StockUnit, ct);
        return new
        {
            movement.Type,
            SaleNumber = await SaleNumberAsync(movement, ct),
            movement.Date,
            unit.UnitNumber,
            unit.ProductName,
            movement.SoldWeight,
            movement.Amount,
            movement.Notes,
        };
    }

    private async Task<string> MovementLabelAsync(StockMovement movement, CancellationToken ct)
    {
        var unit = await UnitInfoAsync(movement.StockUnitId, movement.StockUnit, ct);
        return movement.Type == MovementType.Sale
            ? $"{await SaleNumberAsync(movement, ct)} · {unit.UnitNumber}"
            : $"{OutcomeLabel(movement.Type)} · {unit.UnitNumber}";
    }

    private async Task<string?> SaleNumberAsync(StockMovement movement, CancellationToken ct)
    {
        if (movement.SaleId is null)
        {
            return null;
        }

        return movement.Sale?.SaleNumber ?? await _dbContext.Sales.AsNoTracking()
            .Where(s => s.Id == movement.SaleId)
            .Select(s => s.SaleNumber)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<(string? UnitNumber, string? ProductName)> UnitInfoAsync(
        int stockUnitId, StockUnit? unit, CancellationToken ct)
    {
        unit ??= _dbContext.StockUnits.Local.FirstOrDefault(u => u.Id == stockUnitId);
        var productName = unit?.Batch?.Product?.Name;
        if (unit is not null && productName is not null)
        {
            return (unit.UnitNumber, productName);
        }

        var row = await _dbContext.StockUnits.AsNoTracking()
            .Where(u => u.Id == stockUnitId)
            .Select(u => new { u.UnitNumber, ProductName = u.Batch!.Product!.Name })
            .FirstOrDefaultAsync(ct);

        return (unit?.UnitNumber ?? row?.UnitNumber, productName ?? row?.ProductName);
    }

    private async Task<(string? ProductName, DateOnly ProductionDate)> BatchInfoAsync(
        int batchId, ProductionBatch? batch, CancellationToken ct)
    {
        batch ??= _dbContext.ProductionBatches.Local.FirstOrDefault(b => b.Id == batchId);
        if (batch is not null)
        {
            return (await ProductNameAsync(batch.ProductId, batch.Product, ct), batch.ProductionDate);
        }

        var row = await _dbContext.ProductionBatches.AsNoTracking()
            .Where(b => b.Id == batchId)
            .Select(b => new { ProductName = b.Product!.Name, b.ProductionDate })
            .FirstOrDefaultAsync(ct);

        return (row?.ProductName, row?.ProductionDate ?? default);
    }

    private async Task<string?> ProductNameAsync(int productId, Product? product, CancellationToken ct)
    {
        product ??= _dbContext.Products.Local.FirstOrDefault(p => p.Id == productId);
        return product?.Name ?? await _dbContext.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(ct);
    }

    // --- Outils ----------------------------------------------------------------------------------------

    private IEnumerable<EntityEntry> Changes<T>() => _changes.Where(e => e.Entity is T);

    private List<StockUnit> UnitsOf(ProductionBatch batch, EntityState state) =>
        Changes<StockUnit>()
            .Where(e => e.State == state)
            .Select(e => (StockUnit)e.Entity)
            .Where(u => ReferenceEquals(u.Batch, batch) || (batch.Id != 0 && u.BatchId == batch.Id))
            .ToList();

    private static bool BelongsTo(StockMovement movement, Sale sale) =>
        ReferenceEquals(movement.Sale, sale) || (sale.Id != 0 && movement.SaleId == sale.Id);

    private void MarkHandled(IEnumerable<object> entities)
    {
        foreach (var entity in entities)
        {
            _handled.Add(entity);
        }
    }

    private void Add(
        AuditAction action, AuditEntityType entityType, Func<string?> entityId, string? label,
        object? deletedContent = null, Guid? accountId = null)
    {
        label ??= string.Empty;
        _pending.Add(new PendingAuditEntry
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Label = label.Length > LabelMaxLength ? label[..(LabelMaxLength - 1)] + "…" : label,
            DeletedContent = deletedContent is null ? null : JsonSerializer.Serialize(deletedContent, ContentJson),
            AccountId = accountId ?? _currentAccountId,
        });
    }

    /// <summary>Un changement réel, hors dates d'audit posées par <c>SaveChanges</c> lui-même.</summary>
    private static bool HasMeaningfulChange(EntityEntry entry) =>
        entry.Properties.Any(p =>
            p.Metadata.Name is not ("UpdatedAt" or "CreatedAt")
            && p.IsModified
            && !Equals(p.OriginalValue, p.CurrentValue));

    private static bool IsChanged(EntityEntry entry, string propertyName)
    {
        var property = entry.Property(propertyName);
        return property.IsModified && !Equals(property.OriginalValue, property.CurrentValue);
    }

    private static string Id(int id) => id.ToString(CultureInfo.InvariantCulture);

    /// <summary>Rang d'un numéro d'étiquette <c>CODE-YYMMDD-N</c> ; 0 si le numéro ne suit pas ce format.</summary>
    private static int SequenceOf(string unitNumber) =>
        int.TryParse(unitNumber[(unitNumber.LastIndexOf('-') + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
            ? sequence
            : 0;

    private static string Count(int count, string noun) => $"{count} {noun}{(count > 1 ? "s" : string.Empty)}";

    private static string FormatDate(DateOnly date) => date.ToString("dd/MM/yyyy", French);

    private static string OutcomeLabel(MovementType type) => type switch
    {
        MovementType.Personal => "Perso",
        MovementType.Loss => "Perte",
        _ => "Vente",
    };

    private static string FormatCustomerName(string? firstName, string lastName) =>
        $"{firstName} {lastName}".Trim();

    private static string Snake<TEnum>(TEnum value) where TEnum : struct, Enum =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());
}
