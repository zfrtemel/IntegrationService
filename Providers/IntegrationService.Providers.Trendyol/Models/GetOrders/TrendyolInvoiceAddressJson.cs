using System.Text.Json.Serialization;

namespace IntegrationService.Providers.Trendyol.Models.GetOrders;

internal sealed class TrendyolInvoiceAddressJson : TrendyolAddressJson
{
    [JsonPropertyName("taxOffice")]
    public string? TaxOffice { get; set; }

    [JsonPropertyName("taxNumber")]
    public string? TaxNumber { get; set; }
}
