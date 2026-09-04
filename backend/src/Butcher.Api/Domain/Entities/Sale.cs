namespace Butcher.Api.Domain.Entities;

/// <summary>
/// Une vente : la transaction telle que l'utilisateur la vit (un numéro, une date, un client, un
/// statut de paiement, un total), regroupant un ou plusieurs <see cref="StockMovement"/> — un par
/// unité physique vendue. Pendant, côté vente, de <see cref="ProductionBatch"/> côté production
/// (QM-04, Q-04/Q-05 du PRD).
/// </summary>
public class Sale
{
    public int Id { get; set; }

    public required string SaleNumber { get; set; }

    // Obligatoire : plus de vente anonyme en V1 (RF-17 / RG-07, modifiés le 2026-09-04).
    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public DateTimeOffset Date { get; set; }

    public bool Paid { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedById { get; set; }

    public AppUser? CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
