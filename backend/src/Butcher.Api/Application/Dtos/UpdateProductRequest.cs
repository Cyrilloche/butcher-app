using System.ComponentModel.DataAnnotations;

namespace Butcher.Api.Application.Dtos;

public class UpdateProductRequest
{
    [Required, MaxLength(200)]
    public required string Name { get; set; }
}
