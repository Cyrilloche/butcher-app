using System.ComponentModel.DataAnnotations;

namespace Butcher.Api.Application.Dtos;

public class UpdateCustomerRequest
{
    [Required, MaxLength(100)]
    public required string LastName { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
