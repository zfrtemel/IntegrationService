namespace IntegrationService.Core.Models.Invoices;

public sealed class InvoiceDocumentRequestDto
{
    public string OrderId { get; init; } = string.Empty;
    public string? ExternalInvoiceId { get; init; }
}
