namespace Butcher.Api.Domain.Entities;

public class UnitOfMeasure
{
    public int Id { get; set; }

    public required string Label { get; set; }

    public required string Abbreviation { get; set; }

    public bool IsActive { get; set; } = true;
}
