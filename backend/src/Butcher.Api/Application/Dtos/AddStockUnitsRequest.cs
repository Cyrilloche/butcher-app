namespace Butcher.Api.Application.Dtos;

// Exactement un des deux champs doit être renseigné, selon le sale_mode du produit du lot
// (vérifié dans StockUnitService, pas via Data Annotations : la règle dépend d'une donnée externe à la requête).
public class AddStockUnitsRequest
{
    public List<decimal>? Weights { get; set; }

    public int? Quantity { get; set; }
}
