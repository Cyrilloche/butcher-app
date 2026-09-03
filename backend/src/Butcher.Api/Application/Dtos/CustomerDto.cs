namespace Butcher.Api.Application.Dtos;

public class CustomerDto
{
    public int Id { get; set; }

    public required string LastName { get; set; }

    public string? FirstName { get; set; }

    public string? Phone { get; set; }

    public string? Notes { get; set; }
}
