namespace IntegrationService.Core.Models.Invoices;

public sealed class InvoiceOperationResultDto
{
    public bool Success { get; init; }
    public string? ProviderMessage { get; init; }
    public string? ExternalInvoiceId { get; init; }
    public string? DocumentUrl { get; init; }
}
