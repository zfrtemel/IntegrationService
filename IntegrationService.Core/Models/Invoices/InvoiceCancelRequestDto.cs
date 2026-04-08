namespace IntegrationService.Core.Models.Invoices;

public sealed class InvoiceCancelRequestDto
{
    public string OrderId { get; init; } = string.Empty;
    public string? ExternalInvoiceId { get; init; }
    public string? Reason { get; init; }
}
