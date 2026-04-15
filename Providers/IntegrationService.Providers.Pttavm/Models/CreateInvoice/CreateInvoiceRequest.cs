using System.Text.Json.Serialization;

namespace IntegrationService.Providers.Pttavm.Models.CreateInvoice;

internal sealed class CreateInvoiceRequest
{
    [JsonPropertyName("lineItemId")]
    public List<long> LineItemId { get; set; } = [];

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
