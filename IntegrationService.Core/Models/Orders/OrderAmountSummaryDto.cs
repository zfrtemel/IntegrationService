namespace IntegrationService.Core.Models.Orders;

public sealed class OrderAmountSummaryDto
{
    public decimal Subtotal { get; init; }
    public decimal Shipping { get; init; }
    public decimal DiscountTotal { get; init; }
    public decimal GrandTotal { get; init; }
    public decimal TaxTotal { get; init; }
    public string Currency { get; init; } = "TRY";
}
