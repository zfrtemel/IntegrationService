namespace IntegrationService.Core.Models.Orders;

public sealed class OrderCustomerDto
{
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? PhoneMasked { get; init; }
}
