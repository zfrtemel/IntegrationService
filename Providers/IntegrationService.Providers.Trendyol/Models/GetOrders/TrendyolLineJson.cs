using System.Text.Json.Serialization;

namespace IntegrationService.Providers.Trendyol.Models.GetOrders;

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
