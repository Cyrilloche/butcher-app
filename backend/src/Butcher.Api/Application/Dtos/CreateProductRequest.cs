using System.ComponentModel.DataAnnotations;
using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

public class CreateProductRequest
{
    [Required, MaxLength(20)]
    public required string Code { get; set; }

    [Required, MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    public SaleMode SaleMode { get; set; }

    [Required]
    public int SaleUnitId { get; set; }
}
