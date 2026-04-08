namespace IntegrationService.Core.Models.Invoices;

public sealed class InvoiceDocumentResultDto
{
    public string? DocumentUrl { get; init; }
    public string? PdfBase64 { get; init; }
}
