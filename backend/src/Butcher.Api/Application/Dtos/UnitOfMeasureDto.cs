namespace Butcher.Api.Application.Dtos;

public class UnitOfMeasureDto
{
    public int Id { get; set; }

    public required string Label { get; set; }

    public required string Abbreviation { get; set; }

    public bool IsActive { get; set; }
}
