using System.Text.Json.Serialization;

namespace IntegrationService.Providers.N11.Models.ShipmentPackages;

internal sealed class N11ShipmentPackage
{
    [JsonPropertyName("orderNumber")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("customerEmail")]
    public string? CustomerEmail { get; set; }

    [JsonPropertyName("customerfullName")]
    public string? CustomerFullName { get; set; }

    [JsonPropertyName("taxId")]
    public string? TaxId { get; set; }

    [JsonPropertyName("taxOffice")]
    public string? TaxOffice { get; set; }

    [JsonPropertyName("tcIdentityNumber")]
    public string? TcIdentityNumber { get; set; }

    [JsonPropertyName("lastModifiedDate")]
    public long? LastModifiedDate { get; set; }

    [JsonPropertyName("agreedDeliveryDate")]
    public long? AgreedDeliveryDate { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal? TotalAmount { get; set; }

    [JsonPropertyName("totalDiscountAmount")]
    public decimal? TotalDiscountAmount { get; set; }

    [JsonPropertyName("shipmentPackageStatus")]
    public string? ShipmentPackageStatus { get; set; }

    [JsonPropertyName("billingAddress")]
    public N11Address? BillingAddress { get; set; }

    [JsonPropertyName("shippingAddress")]
    public N11Address? ShippingAddress { get; set; }

    [JsonPropertyName("lines")]
    public List<N11Line>? Lines { get; set; }
}
