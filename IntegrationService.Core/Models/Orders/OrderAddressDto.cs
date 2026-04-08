namespace IntegrationService.Core.Models.Orders;

public sealed class OrderAddressDto
{
    public string? FullAddress { get; init; }
    public string? City { get; init; }
    public string? District { get; init; }
    public string? CountryCode { get; init; }
    public string? PostalCode { get; init; }
}
