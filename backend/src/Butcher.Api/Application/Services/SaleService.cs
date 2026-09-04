using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Butcher.Api.Application.Services;

public class SaleService(AppDbContext dbContext) : ISaleService
{
    private const int MaxSaleNumberAttempts = 3;

    public async Task<List<SaleDto>> GetAllAsync(int? customerId, bool? paid, DateTimeOffset? from, DateTimeOffset? to)
    {
        var query = BaseQuery();

        if (customerId is not null)
        {
            query = query.Where(s => s.CustomerId == customerId);
        }

        if (paid is not null)
        {
            query = query.Where(s => s.Paid == paid);
        }

        if (from is not null)
        {
            query = query.Where(s => s.Date >= from);
        }

        if (to is not null)
        {
            query = query.Where(s => s.Date <= to);
        }

        var sales = await query.OrderByDescending(s => s.Date).ThenByDescending(s => s.Id).ToListAsync();
        return sales.Select(ToDto).ToList();
    }

    public async Task<SaleDto> GetByIdAsync(int id)
    {
        var sale = await FindOrThrowAsync(id);
        return ToDto(sale);
    }

    public async Task<SaleDto> CreateAsync(CreateSaleRequest request)
    {
        if (request.Lines.Count == 0)
        {
            throw new BadRequestException("Une vente doit comporter au moins une ligne.");
        }

        var duplicate = request.Lines
            .GroupBy(l => l.StockUnitId)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new BadRequestException(
                $"L'unité de stock {duplicate.Key} apparaît plusieurs fois dans la même vente.");
        }

        var customer = await dbContext.Customers.FindAsync(request.CustomerId)
            ?? throw new ConflictException($"Le client {request.CustomerId} n'existe pas.");

        var date = request.Date ?? DateTimeOffset.UtcNow;

        // Les unités et leurs statuts sont validés avant toute écriture : la vente est atomique,
        // une ligne en erreur n'en laisse aucune enregistrée.
        var unitIds = request.Lines.Select(l => l.StockUnitId).ToList();
        var units = await dbContext.StockUnits
            .Include(u => u.Batch!).ThenInclude(b => b.Product)
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        foreach (var line in request.Lines)
        {
            if (!units.TryGetValue(line.StockUnitId, out var unit))
            {
                throw new NotFoundException($"Unité de stock {line.StockUnitId} introuvable.");
            }

            StockMovementRules.EnsureCanReceiveMovement(unit);
            StockMovementRules.ValidateSoldWeight(unit, line.SoldWeight);
            StockMovementRules.ValidateAmount(MovementType.Sale, line.Amount);
            StockMovementRules.EnsurePartialSaleIsAllowed(unit, MovementType.Sale, line.IsFullSale);
        }

        for (var attempt = 1; attempt <= MaxSaleNumberAttempts; attempt++)
        {
            var sale = new Sale
            {
                SaleNumber = await GenerateSaleNumberAsync(date),
                CustomerId = customer.Id,
                Customer = customer,
                Date = date,
                Paid = request.Paid,
                Notes = request.Notes,
            };

            foreach (var line in request.Lines)
            {
                var unit = units[line.StockUnitId];

                sale.StockMovements.Add(new StockMovement
                {
                    StockUnitId = unit.Id,
                    StockUnit = unit,
                    Type = MovementType.Sale,
                    Date = date,
                    SoldWeight = line.SoldWeight,
                    Amount = line.Amount,
                    Notes = line.Notes,
                });

                unit.Status = StockMovementRules.DetermineNextStatus(unit.Status, MovementType.Sale, line.IsFullSale);
            }

            dbContext.Sales.Add(sale);

            try
            {
                await dbContext.SaveChangesAsync();
                return ToDto(sale);
            }
            catch (DbUpdateException exception) when (IsSaleNumberConflict(exception) && attempt < MaxSaleNumberAttempts)
            {
                dbContext.Sales.Remove(sale);
                foreach (var movement in sale.StockMovements)
                {
                    dbContext.StockMovements.Remove(movement);
                }
            }
        }

        throw new ConflictException("Impossible de générer un numéro de vente unique, réessayez.");
    }

    public async Task<SaleDto> UpdateAsync(int id, UpdateSaleRequest request)
    {
        var sale = await FindOrThrowAsync(id);

        if (sale.CustomerId != request.CustomerId)
        {
            sale.Customer = await dbContext.Customers.FindAsync(request.CustomerId)
                ?? throw new ConflictException($"Le client {request.CustomerId} n'existe pas.");
            sale.CustomerId = request.CustomerId;
        }

        sale.Date = request.Date;
        sale.Paid = request.Paid;
        sale.Notes = request.Notes;
        sale.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        return ToDto(sale);
    }

    public async Task<SaleDto> SetPaymentAsync(int id, SetSalePaymentRequest request)
    {
        var sale = await FindOrThrowAsync(id);

        sale.Paid = request.Paid;
        sale.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        return ToDto(sale);
    }

    public async Task DeleteAsync(int id)
    {
        var sale = await FindOrThrowAsync(id);

        // Même règle que la suppression d'un mouvement isolé (RG-11) : une unité qui ne porte plus
        // aucun mouvement redevient disponible ; sinon son statut n'est pas recalculé plus finement.
        var unitIds = sale.StockMovements.Select(m => m.StockUnitId).Distinct().ToList();
        var movementIds = sale.StockMovements.Select(m => m.Id).ToList();

        dbContext.StockMovements.RemoveRange(sale.StockMovements);
        dbContext.Sales.Remove(sale);

        var units = await dbContext.StockUnits.Where(u => unitIds.Contains(u.Id)).ToListAsync();
        foreach (var unit in units)
        {
            var remaining = await dbContext.StockMovements
                .CountAsync(m => m.StockUnitId == unit.Id && !movementIds.Contains(m.Id));

            if (remaining == 0)
            {
                unit.Status = StockUnitStatus.Available;
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private IQueryable<Sale> BaseQuery() =>
        dbContext.Sales
            .Include(s => s.Customer)
            .Include(s => s.StockMovements).ThenInclude(m => m.StockUnit!).ThenInclude(u => u.Batch!).ThenInclude(b => b.Product);

    private async Task<Sale> FindOrThrowAsync(int id) =>
        await BaseQuery().FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new NotFoundException($"Vente {id} introuvable.");

    /// <summary>
    /// Numéro communicable `V-YYMMDD-N`, même logique que le numéro de lot (data-model §4.1) :
    /// séquence remise à zéro chaque jour, format court et recopiable à la main.
    /// </summary>
    private async Task<string> GenerateSaleNumberAsync(DateTimeOffset date)
    {
        var day = date.UtcDateTime.Date;
        var nextDay = day.AddDays(1);

        var existingCount = await dbContext.Sales
            .CountAsync(s => s.Date >= day && s.Date < nextDay);

        return $"V-{day:yyMMdd}-{existingCount + 1}";
    }

    private static bool IsSaleNumberConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ix_sale_sale_number" };

    private static SaleDto ToDto(Sale sale) =>
        new()
        {
            Id = sale.Id,
            SaleNumber = sale.SaleNumber,
            CustomerId = sale.CustomerId,
            CustomerName = StockMovementRules.FormatCustomerName(sale.Customer),
            Date = sale.Date,
            Paid = sale.Paid,
            Notes = sale.Notes,
            Total = sale.StockMovements.Sum(m => m.Amount ?? 0m),
            ItemCount = sale.StockMovements.Count,
            Lines = sale.StockMovements
                .OrderBy(m => m.Id)
                .Select(m => new StockMovementDto
                {
                    Id = m.Id,
                    StockUnitId = m.StockUnitId,
                    ProductName = m.StockUnit?.Batch?.Product?.Name,
                    BatchNumber = m.StockUnit?.Batch?.BatchNumber,
                    Type = m.Type,
                    Date = m.Date,
                    SoldWeight = m.SoldWeight,
                    Amount = m.Amount,
                    SaleId = sale.Id,
                    SaleNumber = sale.SaleNumber,
                    CustomerId = sale.CustomerId,
                    CustomerName = StockMovementRules.FormatCustomerName(sale.Customer),
                    Notes = m.Notes,
                })
                .ToList(),
        };
}
