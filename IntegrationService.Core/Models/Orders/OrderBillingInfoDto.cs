namespace IntegrationService.Core.Models.Orders;

public sealed class OrderBillingInfoDto
{
    public string InvoiceType { get; init; } = "Unknown";
    public string? BillingFullNameOrCompany { get; init; }
    public string? TaxOffice { get; init; }
    public string? TaxNumber { get; init; }
    public string? Tckn { get; init; }
    public OrderAddressDto? BillingAddress { get; init; }
    public bool? IsDifferentBillingAddress { get; init; }
    public bool InvoiceAvailable { get; init; }
}
