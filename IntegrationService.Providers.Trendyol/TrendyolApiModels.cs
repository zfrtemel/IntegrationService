using System.Text.Json.Serialization;

namespace IntegrationService.Providers.Trendyol;

internal sealed class TrendyolOrdersPageResponse
{
    [JsonPropertyName("totalElements")]
    public long TotalElements { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("content")]
    public List<TrendyolShipmentPackageJson>? Content { get; set; }
}

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

internal class TrendyolAddressJson
{
    [JsonPropertyName("fullAddress")]
    public string? FullAddress { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("district")]
    public string? District { get; set; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }
}

internal sealed class TrendyolInvoiceAddressJson : TrendyolAddressJson
{
    [JsonPropertyName("taxOffice")]
    public string? TaxOffice { get; set; }

    [JsonPropertyName("taxNumber")]
    public string? TaxNumber { get; set; }
}

internal sealed class TrendyolLineJson
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("lineId")]
    public long? LineId { get; set; }
    [JsonPropertyName("merchantSku")]
    public string? MerchantSku { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("stockCode")]
    public string? StockCode { get; set; }

    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    [JsonPropertyName("price")]
    public double? Price { get; set; }

    [JsonPropertyName("lineUnitPrice")]
    public double? LineUnitPrice { get; set; }

    [JsonPropertyName("amount")]
    public double? Amount { get; set; }

    [JsonPropertyName("lineGrossAmount")]
    public double? LineGrossAmount { get; set; }

    [JsonPropertyName("currencyCode")]
    public string? CurrencyCode { get; set; }

    [JsonPropertyName("vatRate")]
    public double? VatRate { get; set; }
}
