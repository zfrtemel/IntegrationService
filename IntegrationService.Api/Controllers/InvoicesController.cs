using IntegrationService.Application.Services;
using IntegrationService.Core.Models.Invoices;
using IntegrationService.Core.Responses;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.Api.Controllers;

[ApiController]
[Route("api/orders/{orderId}/invoices")]
public sealed class InvoicesController : BaseController
{
    private readonly IInvoiceApplicationService _invoiceApplicationService;

    public InvoicesController(IInvoiceApplicationService invoiceApplicationService)
    {
        _invoiceApplicationService = invoiceApplicationService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(APIResult<InvoiceOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(string orderId, [FromBody] InvoiceCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _invoiceApplicationService.CreateAsync(HttpContext, orderId, request, cancellationToken);
        return ApiResult("Fatura oluşturma sonucu alındı.", true, result);
    }
}
