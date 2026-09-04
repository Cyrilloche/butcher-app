using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

public class ProductDto
{
    public int Id { get; set; }

    public required string Code { get; set; }

    public required string Name { get; set; }

    public SaleMode SaleMode { get; set; }

    public bool AllowPartialSale { get; set; }

    public bool IsActive { get; set; }
}
