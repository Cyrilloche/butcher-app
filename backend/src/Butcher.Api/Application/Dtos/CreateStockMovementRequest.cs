using System.ComponentModel.DataAnnotations;
using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

public class CreateStockMovementRequest
{
    [Required]
    public MovementType Type { get; set; }

    // Requis si et seulement si Type = Sale : une vente appartient toujours à une Sale, qui porte le
    // client, la date et le paiement (RF-17 / RG-07). Créer une vente complète : POST /api/sales.
    public int? SaleId { get; set; }

    // Uniquement pour Type = Sale, et seulement quand l'unité est encore "available" : détermine si la vente
    // finalise l'unité (-> sold) ou démarre une vente à la tranche (-> opened). Ignoré dans les autres cas.
    public bool IsFullSale { get; set; } = true;

    public decimal? SoldWeight { get; set; }

    public decimal? Amount { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
