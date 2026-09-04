namespace Butcher.Api.Application.Dtos;

/// <summary>Bascule « Payée » / « À payer » en un geste, sans renvoyer toute la vente.</summary>
public class SetSalePaymentRequest
{
    public bool Paid { get; set; }
}
