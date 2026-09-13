namespace Butcher.Api.Domain.Enums;

/// <summary>Type de l'objet concerné par une entrée du journal (FR-021). Stocké en <c>snake_case</c>.</summary>
public enum AuditEntityType
{
    Product,
    ProductionBatch,
    StockUnit,
    Sale,
    StockMovement,
    Customer,
    Account,
}
