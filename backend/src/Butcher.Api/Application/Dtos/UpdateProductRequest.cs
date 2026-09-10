using System.ComponentModel.DataAnnotations;
using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

public class UpdateProductRequest
{
    [Required, MaxLength(200)]
    public required string Name { get; set; }

    public bool AllowPartialSale { get; set; }

    /// <summary>
    /// Modifiable tant que le produit n'est rattaché à aucun lot de production. Sur un produit déjà
    /// utilisé, la valeur doit être identique à celle en base : le client peut ainsi renvoyer la
    /// ressource complète sans raisonner sur le gel (FR-003, FR-004).
    /// </summary>
    [Required, MaxLength(20)]
    public required string Code { get; set; }

    /// <summary>
    /// Même règle que <see cref="Code"/> : figé dès le premier lot de production (FR-004).
    /// </summary>
    [Required]
    public SaleMode SaleMode { get; set; }
}
