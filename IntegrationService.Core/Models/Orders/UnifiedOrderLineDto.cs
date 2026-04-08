namespace IntegrationService.Core.Models.Orders;

public sealed class UnifiedOrderLineDto
{
    public string LineId { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Qty { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public decimal? TaxRate { get; init; }
}
