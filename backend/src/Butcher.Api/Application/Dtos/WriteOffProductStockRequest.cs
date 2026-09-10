using System.ComponentModel.DataAnnotations;

namespace Butcher.Api.Application.Dtos;

/// <summary>
/// Solde des unités restantes d'un produit, pour permettre sa désactivation (FR-017, FR-018).
/// </summary>
/// <remarks>
/// Le poids de chaque sortie est calculé par le serveur : il n'a pas à être transmis. Le type de
/// sortie est toujours « perte » sur cette action groupée ; une sortie d'un autre type reste
/// enregistrable unité par unité depuis le détail stock (FR-019).
/// </remarks>
public class WriteOffProductStockRequest
{
    [Required]
    public required List<int> StockUnitIds { get; set; }
}
