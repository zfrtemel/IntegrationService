using IntegrationService.Application.Abstractions;
using IntegrationService.Core.Models.Invoices;
using Microsoft.AspNetCore.Http;

namespace IntegrationService.Application.Services;

public interface IInvoiceApplicationService
{
    Task<InvoiceOperationResultDto> CreateAsync(HttpContext httpContext, string orderId, InvoiceCreateRequestDto request, CancellationToken cancellationToken = default);
}
public sealed class InvoiceApplicationService : IInvoiceApplicationService
{
    private readonly IProviderResolver _providerResolver;

    public InvoiceApplicationService(IProviderResolver providerResolver)
    {
        _providerResolver = providerResolver;
    }

    public async Task<InvoiceOperationResultDto> CreateAsync(HttpContext httpContext, string orderId, InvoiceCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        var resolved = _providerResolver.Resolve(httpContext);

        var payload = new InvoiceCreateRequestDto
        {
            OrderId = orderId,
            ExternalInvoiceNumber = request.ExternalInvoiceNumber,
            InvoiceUrl = request.InvoiceUrl,
            PdfBase64 = request.PdfBase64,
            LineItemIds = request.LineItemIds
        };

        return await resolved.InvoiceProvider.CreateInvoiceAsync(payload, cancellationToken);
    }
}
