using System.Text.Json.Serialization;

namespace IntegrationService.Providers.Trendyol.Models.GetOrders;

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
