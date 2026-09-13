using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos.Reports;

// Rapports de ventes (specs/005-backoffice, contracts §7). Tous les montants sont des sommes des
// montants enregistrés sur les lignes de vente, jamais recalculés (FR-030).

/// <summary>Synthèse d'une période et sa répartition par mois (FR-027).</summary>
public class SalesSummaryDto
{
    public int SaleCount { get; set; }

    public decimal Total { get; set; }

    public decimal PaidTotal { get; set; }

    public decimal PendingTotal { get; set; }

    /// <summary>Mois de Paris portant au moins une vente, du plus ancien au plus récent.</summary>
    public List<MonthlySalesDto> Months { get; set; } = [];
}

public class MonthlySalesDto
{
    /// <summary>`YYYY-MM`.</summary>
    public required string Month { get; set; }

    public int SaleCount { get; set; }

    public decimal Total { get; set; }

    public decimal PaidTotal { get; set; }

    public decimal PendingTotal { get; set; }
}

/// <summary>Ventes d'un client sur la période (FR-028).</summary>
public class CustomerSalesDto
{
    public int CustomerId { get; set; }

    public required string CustomerName { get; set; }

    public int SaleCount { get; set; }

    public decimal Total { get; set; }

    public decimal PendingTotal { get; set; }
}

/// <summary>Ventes d'un produit sur la période (FR-028, clarification du 2026-09-13).</summary>
public class ProductSalesDto
{
    public int ProductId { get; set; }

    public required string ProductCode { get; set; }

    public required string ProductName { get; set; }

    public SaleMode SaleMode { get; set; }

    /// <summary>Unités distinctes vendues ou entamées : un jambon vendu en cinq tranches compte pour une.</summary>
    public int UnitCount { get; set; }

    /// <summary>Lignes de vente : le même jambon compte ici pour cinq.</summary>
    public int LineCount { get; set; }

    /// <summary>Somme des poids vendus, en kilogrammes ; <c>null</c> pour un produit à la pièce.</summary>
    public decimal? SoldWeight { get; set; }

    public decimal Total { get; set; }
}

/// <summary>Montants à encaisser, toutes périodes confondues (FR-029).</summary>
public class ReceivablesDto
{
    public decimal Total { get; set; }

    public List<CustomerReceivableDto> Customers { get; set; } = [];
}

public class CustomerReceivableDto
{
    public int CustomerId { get; set; }

    public required string CustomerName { get; set; }

    public decimal PendingTotal { get; set; }

    public DateTimeOffset OldestUnpaidDate { get; set; }

    /// <summary>Ventes non payées, de la plus ancienne à la plus récente.</summary>
    public List<UnpaidSaleDto> Sales { get; set; } = [];
}

public class UnpaidSaleDto
{
    public int Id { get; set; }

    public required string SaleNumber { get; set; }

    public DateTimeOffset Date { get; set; }

    public decimal Total { get; set; }
}
