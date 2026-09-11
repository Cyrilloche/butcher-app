using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

public class ProductService(AppDbContext dbContext) : IProductService
{
    public async Task<List<ProductDto>> GetAllAsync(bool includeInactive)
    {
        var query = dbContext.Products.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await ProjectToDto(query).ToListAsync();
    }

    public async Task<ProductDto> GetByIdAsync(int id)
    {
        return await ProjectToDto(dbContext.Products.Where(p => p.Id == id)).FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Produit {id} introuvable.");
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request)
    {
        var code = request.Code.ToUpperInvariant();
        await EnsureCodeIsUniqueAsync(code, excludingId: null);
        EnsureAllowPartialSaleIsApplicable(request.SaleMode, request.AllowPartialSale);

        var product = new Product
        {
            Code = code,
            Name = request.Name,
            SaleMode = request.SaleMode,
            AllowPartialSale = request.AllowPartialSale,
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        return await ReadDtoAsync(product.Id);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request)
    {
        var product = await FindOrThrowAsync(id);
        var code = request.Code.ToUpperInvariant();

        // L'état « utilisé » est relu ici, au moment de l'enregistrement, et non tel qu'il était à
        // l'affichage de l'écran (FR-027).
        var isUsed = await dbContext.ProductionBatches.AnyAsync(b => b.ProductId == product.Id);

        if (isUsed)
        {
            EnsureIdentityIsUnchanged(product, code, request.SaleMode);
        }

        // Le mode de vente demandé, et non celui en base : sans cela un passage en « à la pièce »
        // avec vente à la tranche activée dans la même requête passerait au travers (FR-008).
        var saleMode = isUsed ? product.SaleMode : request.SaleMode;
        var allowPartialSale = request.AllowPartialSale;

        // Le passage du poids vers la pièce éteint l'autorisation de vente à la tranche, les deux
        // étant incompatibles (FR-009). C'est une conséquence du changement de mode, pas une erreur
        // de saisie : sur un produit déjà à la pièce, la même demande reste un refus (FR-008).
        var switchesToByPiece = product.SaleMode == SaleMode.ByWeight && saleMode == SaleMode.ByPiece;
        if (switchesToByPiece)
        {
            allowPartialSale = false;
        }

        EnsureAllowPartialSaleIsApplicable(saleMode, allowPartialSale);
        await EnsureCodeIsUniqueAsync(code, excludingId: product.Id);

        product.Name = request.Name;
        product.Code = code;
        product.SaleMode = saleMode;
        product.AllowPartialSale = allowPartialSale;
        await dbContext.SaveChangesAsync();

        return await ReadDtoAsync(product.Id);
    }

    /// <summary>
    /// Désactive un produit, à condition qu'il ne lui reste aucune unité disponible ou entamée
    /// (FR-016). Sans effet sur un produit déjà désactivé.
    /// </summary>
    public async Task DeactivateAsync(int id)
    {
        var product = await FindOrThrowAsync(id);

        if (!product.IsActive)
        {
            return;
        }

        var remaining = await RemainingStockUnitsQuery(product.Id).CountAsync();

        if (remaining > 0)
        {
            throw new ConflictException(
                $"Le produit « {product.Name} » ne peut pas être désactivé : "
                + (remaining > 1
                    ? $"{remaining} unités restent à écouler."
                    : "1 unité reste à écouler.")
                + " Vendez-les, ou soldez-les depuis la fiche du produit.");
        }

        product.IsActive = false;
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Solde une sélection d'unités restantes en sorties de type « perte », pour rendre possible la
    /// désactivation du produit (FR-017 à FR-021).
    /// </summary>
    /// <remarks>
    /// Le poids de chaque sortie est calculé par le serveur, jamais transmis par le client. Une unité
    /// entamée dont les ventes couvrent déjà tout le poids pesé a un restant nul : elle est clôturée
    /// comme vendue plutôt que de recevoir une perte de poids nul, que la validation des mouvements
    /// rejetterait. Les unités déjà sorties de la sélection sont ignorées, et une sélection vide reste
    /// sans effet, plutôt que de faire échouer l'opération.
    /// </remarks>
    public async Task<WriteOffProductStockResult> WriteOffStockAsync(int id, WriteOffProductStockRequest request)
    {
        var product = await FindOrThrowAsync(id);

        var units = await dbContext.StockUnits
            .Include(u => u.Batch!).ThenInclude(b => b.Product)
            .Where(u => request.StockUnitIds.Contains(u.Id))
            .ToListAsync();

        var foreign = units.FirstOrDefault(u => u.Batch!.ProductId != product.Id);
        if (foreign is not null)
        {
            throw new BadRequestException(
                $"L'unité {foreign.Id} n'appartient pas au produit « {product.Name} » et ne peut pas être soldée ici.");
        }

        var toWriteOff = units
            .Where(u => u.Status is StockUnitStatus.Available or StockUnitStatus.Opened)
            .ToList();

        if (toWriteOff.Count == 0)
        {
            return new WriteOffProductStockResult { WrittenOffCount = 0 };
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var now = DateTimeOffset.UtcNow;

        foreach (var unit in toWriteOff)
        {
            var soldWeight = StockMovementRules.ComputeRemainingWeight(
                unit, await SumSoldWeightAsync(unit.Id));

            if (unit.Weight is not null && soldWeight == 0m)
            {
                // Plus rien à sortir : l'unité a été vendue en totalité, on la clôture.
                unit.Status = StockUnitStatus.Sold;
                continue;
            }

            dbContext.StockMovements.Add(new StockMovement
            {
                StockUnitId = unit.Id,
                Type = MovementType.Loss,
                Date = now,
                SoldWeight = soldWeight,
            });

            unit.Status = StockMovementRules.DetermineNextStatus(unit.Status, MovementType.Loss, isFullSale: false);
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return new WriteOffProductStockResult { WrittenOffCount = toWriteOff.Count };
    }

    private IQueryable<StockUnit> RemainingStockUnitsQuery(int productId) =>
        dbContext.StockUnits.Where(u =>
            u.Batch!.ProductId == productId
            && (u.Status == StockUnitStatus.Available || u.Status == StockUnitStatus.Opened));

    private async Task<decimal> SumSoldWeightAsync(int stockUnitId) =>
        await dbContext.StockMovements
            .Where(m => m.StockUnitId == stockUnitId && m.Type == MovementType.Sale)
            .SumAsync(m => m.SoldWeight ?? 0m);

    public async Task ReactivateAsync(int id)
    {
        var product = await FindOrThrowAsync(id);
        product.IsActive = true;
        await dbContext.SaveChangesAsync();
    }

    private async Task<Product> FindOrThrowAsync(int id) =>
        await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Produit {id} introuvable.");

    private async Task EnsureCodeIsUniqueAsync(string code, int? excludingId)
    {
        var exists = await dbContext.Products
            .Where(p => excludingId == null || p.Id != excludingId)
            .AnyAsync(p => p.Code == code);

        if (exists)
        {
            throw new ConflictException($"Un produit avec le code « {code} » existe déjà.");
        }
    }

    /// <summary>
    /// Sur un produit rattaché à au moins un lot, le code et le mode de vente sont figés : le code
    /// apparaît dans des numéros de lot recopiés à la main sur des étiquettes, et le mode de vente
    /// détermine la lecture des ventes passées. Une valeur identique à celle en base est acceptée,
    /// afin que le client puisse renvoyer la ressource complète (FR-004).
    /// </summary>
    private static void EnsureIdentityIsUnchanged(Product product, string code, SaleMode saleMode)
    {
        if (code != product.Code)
        {
            throw new ConflictException(
                $"Le code du produit « {product.Name} » ne peut plus être modifié : il a déjà servi à "
                + "fabriquer au moins un lot. Désactivez ce produit et créez-en un nouveau.");
        }

        if (saleMode != product.SaleMode)
        {
            throw new ConflictException(
                $"Le mode de vente du produit « {product.Name} » ne peut plus être modifié : il a déjà "
                + "servi à fabriquer au moins un lot. Désactivez ce produit et créez-en un nouveau.");
        }
    }

    private static void EnsureAllowPartialSaleIsApplicable(SaleMode saleMode, bool allowPartialSale)
    {
        if (allowPartialSale && saleMode != SaleMode.ByWeight)
        {
            throw new BadRequestException(
                "« AllowPartialSale » n'est applicable qu'aux produits vendus au poids.");
        }
    }

    /// <summary>
    /// Projection unique du produit vers son DTO. « IsUsed » et « RemainingStockUnitCount » sont des
    /// états dérivés, calculés ici et jamais stockés (FR-001, FR-002) : une colonne dénormalisée
    /// devrait être maintenue à la création comme à la suppression d'un lot, et divergerait au premier
    /// oubli.
    /// </summary>
    private static IQueryable<ProductDto> ProjectToDto(IQueryable<Product> query) =>
        query.Select(product => new ProductDto
        {
            Id = product.Id,
            Code = product.Code,
            Name = product.Name,
            SaleMode = product.SaleMode,
            AllowPartialSale = product.AllowPartialSale,
            IsActive = product.IsActive,
            IsUsed = product.ProductionBatches.Any(),
            RemainingStockUnitCount = product.ProductionBatches
                .SelectMany(batch => batch.StockUnits)
                .Count(unit =>
                    unit.Status == StockUnitStatus.Available || unit.Status == StockUnitStatus.Opened),
        });

    private async Task<ProductDto> ReadDtoAsync(int id) =>
        await ProjectToDto(dbContext.Products.Where(p => p.Id == id)).FirstAsync();
}
