using System.ComponentModel.DataAnnotations;

namespace Butcher.Api.Application.Dtos;

public class CreateSaleRequest
{
    // Obligatoire : plus de vente anonyme (RF-17 / RG-07).
    [Required]
    public int CustomerId { get; set; }

    /// <summary>Date de la vente ; par défaut, maintenant.</summary>
    public DateTimeOffset? Date { get; set; }

    public bool Paid { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    // Contrairement au lot de production (dont les unités sont ajoutées en plusieurs fois, la pesée
    // pouvant s'étaler sur plusieurs jours), une vente est un instant unique : elle est créée avec
    // ses lignes, en une transaction. Des lignes peuvent être ajoutées ensuite via
    // POST /api/stock-units/{id}/movements en passant le saleId.
    [Required]
    [MinLength(1, ErrorMessage = "Une vente doit comporter au moins une ligne.")]
    public List<CreateSaleLineRequest> Lines { get; set; } = [];
}
