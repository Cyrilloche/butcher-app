using System.ComponentModel.DataAnnotations;

namespace Butcher.Api.Application.Dtos;

public class UpdateUnitOfMeasureRequest
{
    [Required, MaxLength(100)]
    public required string Label { get; set; }

    [Required, MaxLength(20)]
    public required string Abbreviation { get; set; }
}
