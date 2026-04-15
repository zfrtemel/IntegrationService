using System.Text.Json.Serialization;

namespace IntegrationService.Providers.Pttavm.Models.CreateInvoice;

internal sealed class CreateInvoiceResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error_Message")]
    public string? ErrorMessage { get; set; }
}
