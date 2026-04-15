using System.Text.Json.Serialization;

namespace IntegrationService.Providers.N11.Models.ShipmentPackages;

internal sealed class N11Line
{
    [JsonPropertyName("orderLineId")]
    public long? OrderLineId { get; set; }

    [JsonPropertyName("stockCode")]
    public string? StockCode { get; set; }

    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("sellerInvoiceAmount")]
    public decimal? SellerInvoiceAmount { get; set; }

    [JsonPropertyName("totalSellerDiscountPrice")]
    public decimal? TotalSellerDiscountPrice { get; set; }

    [JsonPropertyName("vatRate")]
    public decimal? VatRate { get; set; }

    [JsonPropertyName("orderItemLineItemStatusName")]
    public string? OrderItemLineItemStatusName { get; set; }
}
