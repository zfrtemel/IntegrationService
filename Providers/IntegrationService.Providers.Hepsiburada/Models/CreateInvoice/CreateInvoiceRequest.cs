using System.Text.Json.Serialization;

namespace IntegrationService.Providers.Hepsiburada.Models.CreateInvoice;

internal sealed class CreateInvoiceRequest
{
    [JsonPropertyName("arrangementDate")]
    public string ArrangementDate { get; set; } = "";

    [JsonPropertyName("invoiceLink")]
    public string InvoiceLink { get; set; } = "";

    [JsonPropertyName("rowNumber")]
    public string RowNumber { get; set; } = "";

    [JsonPropertyName("serialNumber")]
    public string SerialNumber { get; set; } = "";

    [JsonPropertyName("invoices")]
    public CreateInvoiceRequestItem[] Invoices { get; set; } = [];
}

internal sealed class CreateInvoiceRequestItem
{
    [JsonPropertyName("arrangementDate")]
    public string ArrangementDate { get; set; } = "";

    [JsonPropertyName("invoiceLink")]
    public string InvoiceLink { get; set; } = "";

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = "";

    [JsonPropertyName("rowNumber")]
    public string RowNumber { get; set; } = "";

    [JsonPropertyName("serialNumber")]
    public string SerialNumber { get; set; } = "";
}
