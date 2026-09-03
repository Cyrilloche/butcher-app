using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

public class StockMovementService(AppDbContext dbContext) : IStockMovementService
{
    public async Task<List<StockMovementDto>> GetAllAsync(int? stockUnitId, int? customerId)
    {
        var query = dbContext.StockMovements.Include(m => m.Customer).AsQueryable();

        if (stockUnitId is not null)
        {
            query = query.Where(m => m.StockUnitId == stockUnitId);
        }

        if (customerId is not null)
        {
            query = query.Where(m => m.CustomerId == customerId);
        }

        return await query.OrderByDescending(m => m.Date).Select(m => ToDto(m)).ToListAsync();
    }

    public async Task<StockMovementDto> GetByIdAsync(int id)
    {
        var movement = await FindOrThrowAsync(id);
        return ToDto(movement);
    }

    public async Task<StockMovementDto> CreateAsync(int stockUnitId, CreateStockMovementRequest request)
    {
        var unit = await dbContext.StockUnits
            .Include(u => u.Batch!).ThenInclude(b => b.Product)
            .FirstOrDefaultAsync(u => u.Id == stockUnitId)
            ?? throw new NotFoundException($"Unité de stock {stockUnitId} introuvable.");

        if (unit.Status is not (StockUnitStatus.Available or StockUnitStatus.Opened))
        {
            throw new ConflictException($"Cette unité est déjà finalisée (statut « {unit.Status} ») et ne peut plus recevoir de mouvement.");
        }

        ValidateSoldWeight(unit, request.SoldWeight);
        var customer = await ValidateAndResolveCustomerAsync(request.Type, request.CustomerId, request.Amount);

        var movement = new StockMovement
        {
            StockUnitId = unit.Id,
            Type = request.Type,
            Date = DateTimeOffset.UtcNow,
            SoldWeight = request.SoldWeight,
            Amount = request.Amount,
            CustomerId = customer?.Id,
            Customer = customer,
            Notes = request.Notes,
        };

        unit.Status = DetermineNextStatus(unit.Status, request.Type, request.IsFullSale);

        dbContext.StockMovements.Add(movement);
        await dbContext.SaveChangesAsync();

        return ToDto(movement);
    }

    public async Task<StockMovementDto> UpdateAsync(int id, UpdateStockMovementRequest request)
    {
        var movement = await FindOrThrowAsync(id);
        var unit = await dbContext.StockUnits
            .Include(u => u.Batch!).ThenInclude(b => b.Product)
            .FirstAsync(u => u.Id == movement.StockUnitId);

        ValidateSoldWeight(unit, request.SoldWeight);
        var customer = await ValidateAndResolveCustomerAsync(movement.Type, request.CustomerId, request.Amount);

        movement.SoldWeight = request.SoldWeight;
        movement.Amount = request.Amount;
        movement.CustomerId = customer?.Id;
        movement.Customer = customer;
        movement.Notes = request.Notes;
        await dbContext.SaveChangesAsync();

        return ToDto(movement);
    }

    public async Task DeleteAsync(int id)
    {
        var movement = await FindOrThrowAsync(id);
        var unit = await dbContext.StockUnits.FirstAsync(u => u.Id == movement.StockUnitId);

        dbContext.StockMovements.Remove(movement);

        var remainingMovements = await dbContext.StockMovements
            .CountAsync(m => m.StockUnitId == unit.Id && m.Id != id);

        // Règle volontairement simple : si plus aucun mouvement ne subsiste sur l'unité, elle redevient
        // disponible (annule une saisie erronée). Dans le cas contraire (ex. une vente partielle parmi
        // d'autres), le statut n'est pas recalculé plus finement — une clôture déjà faite ne "rouvre" pas.
        if (remainingMovements == 0)
        {
            unit.Status = StockUnitStatus.Available;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task CloseAsync(int stockUnitId)
    {
        var unit = await dbContext.StockUnits.FindAsync(stockUnitId)
            ?? throw new NotFoundException($"Unité de stock {stockUnitId} introuvable.");

        if (unit.Status != StockUnitStatus.Opened)
        {
            throw new ConflictException($"Seule une unité au statut « opened » peut être clôturée (statut actuel : « {unit.Status} »).");
        }

        unit.Status = StockUnitStatus.Sold;
        await dbContext.SaveChangesAsync();
    }

    private static void ValidateSoldWeight(StockUnit unit, decimal? soldWeight)
    {
        var saleMode = unit.Batch!.Product!.SaleMode;

        if (saleMode == SaleMode.ByWeight)
        {
            if (soldWeight is null or <= 0)
            {
                throw new BadRequestException("« SoldWeight » est requis et doit être positif pour un produit vendu au poids.");
            }
        }
        else if (soldWeight is not null)
        {
            throw new BadRequestException("« SoldWeight » n'est pas applicable pour un produit vendu à la pièce.");
        }
    }

    private async Task<Customer?> ValidateAndResolveCustomerAsync(MovementType type, int? customerId, decimal? amount)
    {
        if (type == MovementType.Sale)
        {
            if (amount is null or <= 0)
            {
                throw new BadRequestException("« Amount » est requis et doit être positif pour une vente.");
            }

            if (customerId is null)
            {
                return null;
            }

            return await dbContext.Customers.FindAsync(customerId)
                ?? throw new ConflictException($"Le client {customerId} n'existe pas.");
        }

        if (amount is not null)
        {
            throw new BadRequestException("« Amount » n'est applicable que pour une vente.");
        }

        if (customerId is not null)
        {
            throw new BadRequestException("« CustomerId » n'est applicable que pour une vente.");
        }

        return null;
    }

    private static StockUnitStatus DetermineNextStatus(StockUnitStatus currentStatus, MovementType type, bool isFullSale) =>
        (currentStatus, type) switch
        {
            (StockUnitStatus.Available, MovementType.Sale) => isFullSale ? StockUnitStatus.Sold : StockUnitStatus.Opened,
            (StockUnitStatus.Opened, MovementType.Sale) => StockUnitStatus.Opened,
            (_, MovementType.Personal) => StockUnitStatus.Personal,
            (_, MovementType.Loss) => StockUnitStatus.Lost,
            _ => currentStatus,
        };

    private async Task<StockMovement> FindOrThrowAsync(int id) =>
        await dbContext.StockMovements.Include(m => m.Customer).FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new NotFoundException($"Mouvement de stock {id} introuvable.");

    private static StockMovementDto ToDto(StockMovement movement) =>
        new()
        {
            Id = movement.Id,
            StockUnitId = movement.StockUnitId,
            Type = movement.Type,
            Date = movement.Date,
            SoldWeight = movement.SoldWeight,
            Amount = movement.Amount,
            CustomerId = movement.CustomerId,
            CustomerName = movement.Customer is null ? null : $"{movement.Customer.FirstName} {movement.Customer.LastName}".Trim(),
            Notes = movement.Notes,
        };
}
