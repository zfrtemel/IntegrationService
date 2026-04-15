using System.Text.Json.Serialization;

namespace IntegrationService.Providers.N11.Models.ShipmentPackages;

internal sealed class N11Address
{
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("district")]
    public string? District { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("gsm")]
    public string? Gsm { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("taxId")]
    public string? TaxId { get; set; }

    [JsonPropertyName("taxHouse")]
    public string? TaxHouse { get; set; }

    [JsonPropertyName("invoiceType")]
    public int? InvoiceType { get; set; }

    [JsonPropertyName("tcId")]
    public string? TcId { get; set; }
}
