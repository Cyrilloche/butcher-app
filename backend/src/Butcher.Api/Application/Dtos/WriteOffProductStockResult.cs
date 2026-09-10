namespace Butcher.Api.Application.Dtos;

public class WriteOffProductStockResult
{
    /// <summary>Nombre d'unités effectivement sorties du stock par le solde.</summary>
    public int WrittenOffCount { get; set; }
}
