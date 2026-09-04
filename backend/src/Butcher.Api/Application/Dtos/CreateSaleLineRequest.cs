using System.ComponentModel.DataAnnotations;

namespace Butcher.Api.Application.Dtos;

public class CreateSaleLineRequest
{
    [Required]
    public int StockUnitId { get; set; }

    // Détermine, quand l'unité est encore "available", si la vente la finalise (-> sold) ou démarre
    // une vente à la tranche (-> opened). Ignoré si l'unité est déjà "opened" (RG-04).
    public bool IsFullSale { get; set; } = true;

    public decimal? SoldWeight { get; set; }

    /// <summary>Montant réellement encaissé, pré-rempli côté client par poids × prix du lot (RG-03), stocké tel quel.</summary>
    [Required]
    public decimal Amount { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
