namespace Butcher.Api.Domain.Entities;

public class Customer
{
    public int Id { get; set; }

    public required string LastName { get; set; }

    public string? FirstName { get; set; }

    public string? Phone { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
