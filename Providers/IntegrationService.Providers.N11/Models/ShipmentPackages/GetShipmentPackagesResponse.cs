using System.Text.Json.Serialization;

namespace IntegrationService.Providers.N11.Models.ShipmentPackages;

internal sealed class N11ShipmentPackagesResponse
{
    [JsonPropertyName("pageCount")]
    public int? PageCount { get; set; }

    [JsonPropertyName("totalPages")]
    public int? TotalPages { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("size")]
    public int? Size { get; set; }

    [JsonPropertyName("content")]
    public List<N11ShipmentPackage>? Content { get; set; }
}
