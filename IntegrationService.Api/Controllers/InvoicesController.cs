using IntegrationService.Api.Infrastructure;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Models.Invoices;
using IntegrationService.Core.Responses;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.Api.Controllers;

[ApiController]
[Route("api/orders/{orderId}/invoices")]
public sealed class InvoicesController : BaseController
{
    private readonly IOrderProviderFactory _providerFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public InvoicesController(IOrderProviderFactory providerFactory, IHttpContextAccessor httpContextAccessor)
    {
        _providerFactory = providerFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost]
    [ProducesResponseType(typeof(APIResult<InvoiceOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(string orderId, [FromBody] InvoiceCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        var ctx = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext bulunamadı.");
        var resolved = _providerFactory.Resolve(ctx);

        if (!resolved.Capabilities.SupportsInvoiceCreate)
            throw new NotSupportedByProviderException("Bu provider fatura oluşturma operasyonunu desteklemiyor.");

        var payload = new InvoiceCreateRequestDto
        {
            OrderId = orderId,
            ExternalInvoiceNumber = request.ExternalInvoiceNumber,
            InvoiceUrl = request.InvoiceUrl,
            PdfBase64 = request.PdfBase64,
            LineItemIds = request.LineItemIds
        };
        var result = await resolved.InvoiceProvider.CreateInvoiceAsync(payload, cancellationToken);
        return ApiResponse(ApiResult("Fatura oluşturma sonucu alındı.", true, result));
    }

    [HttpPost("cancel")]
    [ProducesResponseType(typeof(APIResult<InvoiceOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(string orderId, [FromBody] InvoiceCancelRequestDto request, CancellationToken cancellationToken = default)
    {
        var ctx = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext bulunamadı.");
        var resolved = _providerFactory.Resolve(ctx);

        if (!resolved.Capabilities.SupportsInvoiceCancel)
            throw new NotSupportedByProviderException("Bu provider fatura iptal operasyonunu desteklemiyor.");

        var payload = new InvoiceCancelRequestDto
        {
            OrderId = orderId,
            ExternalInvoiceId = request.ExternalInvoiceId,
            Reason = request.Reason
        };
        var result = await resolved.InvoiceProvider.CancelInvoiceAsync(payload, cancellationToken);
        return ApiResponse(ApiResult("Fatura iptal sonucu alındı.", true, result));
    }

    [HttpGet("document")]
    [ProducesResponseType(typeof(APIResult<InvoiceDocumentResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Document(string orderId, [FromQuery] string? externalInvoiceId = null, CancellationToken cancellationToken = default)
    {
        var ctx = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext bulunamadı.");
        var resolved = _providerFactory.Resolve(ctx);

        if (!resolved.Capabilities.SupportsInvoiceDownload)
            throw new NotSupportedByProviderException("Bu provider fatura dökümanı indirme operasyonunu desteklemiyor.");

        var request = new InvoiceDocumentRequestDto
        {
            OrderId = orderId,
            ExternalInvoiceId = externalInvoiceId
        };

        var result = await resolved.InvoiceProvider.GetInvoiceDocumentAsync(request, cancellationToken);
        return ApiResponse(ApiResult("Fatura dökümanı sonucu alındı.", true, result));
    }
}
