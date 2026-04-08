using IntegrationService.Core.Models.Invoices;

namespace IntegrationService.Core.Contracts;

public interface IInvoiceProvider
{
    Task<InvoiceOperationResultDto> CreateInvoiceAsync(InvoiceCreateRequestDto request, CancellationToken cancellationToken = default);
    Task<InvoiceOperationResultDto> CancelInvoiceAsync(InvoiceCancelRequestDto request, CancellationToken cancellationToken = default);
    Task<InvoiceDocumentResultDto> GetInvoiceDocumentAsync(InvoiceDocumentRequestDto request, CancellationToken cancellationToken = default);
}
