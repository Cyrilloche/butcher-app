using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

public class UnitOfMeasureService(AppDbContext dbContext) : IUnitOfMeasureService
{
    public async Task<List<UnitOfMeasureDto>> GetAllAsync(bool includeInactive)
    {
        var query = dbContext.UnitsOfMeasure.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }

        return await query.Select(u => ToDto(u)).ToListAsync();
    }

    public async Task<UnitOfMeasureDto> GetByIdAsync(int id)
    {
        var unit = await FindOrThrowAsync(id);
        return ToDto(unit);
    }

    public async Task<UnitOfMeasureDto> CreateAsync(CreateUnitOfMeasureRequest request)
    {
        await EnsureLabelAndAbbreviationAreUniqueAsync(request.Label, request.Abbreviation, excludingId: null);

        var unit = new UnitOfMeasure { Label = request.Label, Abbreviation = request.Abbreviation };

        dbContext.UnitsOfMeasure.Add(unit);
        await dbContext.SaveChangesAsync();

        return ToDto(unit);
    }

    public async Task<UnitOfMeasureDto> UpdateAsync(int id, UpdateUnitOfMeasureRequest request)
    {
        var unit = await FindOrThrowAsync(id);

        await EnsureLabelAndAbbreviationAreUniqueAsync(request.Label, request.Abbreviation, excludingId: id);

        unit.Label = request.Label;
        unit.Abbreviation = request.Abbreviation;
        await dbContext.SaveChangesAsync();

        return ToDto(unit);
    }

    public async Task DeactivateAsync(int id)
    {
        var unit = await FindOrThrowAsync(id);

        var isUsedByActiveProduct = await dbContext.Products.AnyAsync(p => p.SaleUnitId == id && p.IsActive);
        if (isUsedByActiveProduct)
        {
            throw new ConflictException(
                $"L'unité « {unit.Label} » est utilisée par au moins un produit actif et ne peut pas être désactivée.");
        }

        unit.IsActive = false;
        await dbContext.SaveChangesAsync();
    }

    public async Task ReactivateAsync(int id)
    {
        var unit = await FindOrThrowAsync(id);

        unit.IsActive = true;
        await dbContext.SaveChangesAsync();
    }

    private async Task<UnitOfMeasure> FindOrThrowAsync(int id) =>
        await dbContext.UnitsOfMeasure.FindAsync(id)
            ?? throw new NotFoundException($"Unité de mesure {id} introuvable.");

    private async Task EnsureLabelAndAbbreviationAreUniqueAsync(string label, string abbreviation, int? excludingId)
    {
        var conflict = await dbContext.UnitsOfMeasure
            .Where(u => excludingId == null || u.Id != excludingId)
            .Where(u => u.Label == label || u.Abbreviation == abbreviation)
            .FirstOrDefaultAsync();

        if (conflict is not null)
        {
            throw new ConflictException(
                $"Une unité de mesure avec le libellé « {label} » ou l'abréviation « {abbreviation} » existe déjà.");
        }
    }

    private static UnitOfMeasureDto ToDto(UnitOfMeasure unit) =>
        new()
        {
            Id = unit.Id,
            Label = unit.Label,
            Abbreviation = unit.Abbreviation,
            IsActive = unit.IsActive,
        };
}
