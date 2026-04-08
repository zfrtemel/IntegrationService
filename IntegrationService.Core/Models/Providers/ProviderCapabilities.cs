namespace IntegrationService.Core.Models.Providers;

public sealed class ProviderCapabilities
{
    public bool SupportsInvoiceCreate { get; init; }
    public bool SupportsInvoiceCancel { get; init; }
    public bool SupportsInvoiceDownload { get; init; }
    public bool SupportsOrderBillingFields { get; init; }
}
