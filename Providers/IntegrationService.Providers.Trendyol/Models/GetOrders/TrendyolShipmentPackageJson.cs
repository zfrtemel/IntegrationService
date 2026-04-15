using System.Text.Json.Serialization;

namespace IntegrationService.Providers.Trendyol.Models.GetOrders;

internal sealed class TrendyolShipmentPackageJson
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("shipmentPackageId")]
    public long? ShipmentPackageId { get; set; }

    [JsonPropertyName("orderNumber")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("shipmentPackageStatus")]
    public string? ShipmentPackageStatus { get; set; }

    [JsonPropertyName("grossAmount")]
    public double? GrossAmount { get; set; }

    [JsonPropertyName("packageGrossAmount")]
    public double? PackageGrossAmount { get; set; }

    [JsonPropertyName("packageTotalDiscount")]
    public double? PackageTotalDiscount { get; set; }

    [JsonPropertyName("orderDate")]
    public long? OrderDate { get; set; }

    [JsonPropertyName("currencyCode")]
    public string? CurrencyCode { get; set; }

    [JsonPropertyName("customerFirstName")]
    public string? CustomerFirstName { get; set; }

    [JsonPropertyName("customerLastName")]
    public string? CustomerLastName { get; set; }

    [JsonPropertyName("customerEmail")]
    public string? CustomerEmail { get; set; }

    [JsonPropertyName("shipmentAddress")]
    public TrendyolAddressJson? ShipmentAddress { get; set; }

    [JsonPropertyName("invoiceAddress")]
    public TrendyolInvoiceAddressJson? InvoiceAddress { get; set; }

    [JsonPropertyName("lines")]
    public List<TrendyolLineJson>? Lines { get; set; }
}
