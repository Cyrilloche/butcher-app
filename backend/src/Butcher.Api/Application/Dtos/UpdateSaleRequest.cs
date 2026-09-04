using System.ComponentModel.DataAnnotations;

namespace Butcher.Api.Application.Dtos;

public class UpdateSaleRequest
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public DateTimeOffset Date { get; set; }

    public bool Paid { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
