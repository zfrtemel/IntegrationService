namespace IntegrationService.Core.Models.Invoices;

public sealed class InvoiceCreateRequestDto
{
    public string OrderId { get; init; } = string.Empty;
    public IReadOnlyList<long> LineItemIds { get; init; } = Array.Empty<long>();
    public string? PdfBase64 { get; init; }
    public string? InvoiceUrl { get; init; }
    public string? ExternalInvoiceNumber { get; init; }
}
