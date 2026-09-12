namespace Butcher.Api.Application.Dtos;

public class SaleDto
{
    public int Id { get; set; }

    public required string SaleNumber { get; set; }

    public int CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public DateTimeOffset Date { get; set; }

    public bool Paid { get; set; }

    public string? Notes { get; set; }

    /// <summary>Somme des montants réellement encaissés sur les lignes (RG-05 : jamais recalculé).</summary>
    public decimal Total { get; set; }

    public int ItemCount { get; set; }

    /// <summary>
    /// Nom affiché du compte qui a enregistré la vente, ou <c>null</c> pour une vente antérieure aux
    /// comptes nominatifs (RF-27, FR-020a).
    /// </summary>
    public string? CreatedByName { get; set; }

    public List<StockMovementDto> Lines { get; set; } = [];
}
