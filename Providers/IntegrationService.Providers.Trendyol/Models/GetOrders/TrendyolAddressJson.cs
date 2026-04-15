using System.Text.Json.Serialization;

namespace IntegrationService.Providers.Trendyol.Models.GetOrders;

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
