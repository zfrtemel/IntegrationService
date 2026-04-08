using IntegrationService.Core.Providers;

namespace IntegrationService.Core.Models.Orders;

public sealed class UnifiedOrderDto
{
    public string OrderId { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public IntegrationProviderType Marketplace { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public OrderCustomerDto Customer { get; init; } = new();
    public OrderAmountSummaryDto Amounts { get; init; } = new();
    public OrderAddressDto ShippingAddress { get; init; } = new();
    public OrderBillingInfoDto BillingInfo { get; init; } = new();
    public IReadOnlyList<UnifiedOrderLineDto> Lines { get; init; } = Array.Empty<UnifiedOrderLineDto>();
}
