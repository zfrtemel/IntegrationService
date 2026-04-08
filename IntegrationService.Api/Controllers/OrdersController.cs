using System.Net;
using IntegrationService.Api.Infrastructure;
using IntegrationService.Core.Models.Orders;
using IntegrationService.Core.Responses;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : BaseController
{
    private readonly IOrderProviderFactory _providerFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrdersController(IOrderProviderFactory providerFactory, IHttpContextAccessor httpContextAccessor)
    {
        _providerFactory = providerFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    [ProducesResponseType(typeof(APIResult<UnifiedOrderPageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 0,
        [FromQuery] int size = 50,
        [FromQuery] long? startDateUnixMs = null,
        [FromQuery] long? endDateUnixMs = null,
        [FromQuery] string? status = null,
        [FromQuery] string? orderNumber = null,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext bulunamadı.");

        var resolved = _providerFactory.Resolve(httpContext);
        var query = new UnifiedOrderQuery
        {
            Page = page,
            Size = size,
            StartDateUnixMs = startDateUnixMs,
            EndDateUnixMs = endDateUnixMs,
            Status = status,
            OrderNumber = orderNumber
        };

        var data = await resolved.OrderProvider.GetOrdersAsync(query, cancellationToken);
        return ApiResponse(ApiResult("Sipariş listesi alındı.", true, data));
    }

    [HttpGet("{externalOrderId}")]
    [ProducesResponseType(typeof(APIResult<UnifiedOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(APIResult<UnifiedOrderDto?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detail([FromRoute] string externalOrderId, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext bulunamadı.");

        var resolved = _providerFactory.Resolve(httpContext);
        var detail = await resolved.OrderProvider.GetOrderDetailAsync(externalOrderId, cancellationToken);

        if (detail is null)
        {
            return ApiResponse(ApiResult<UnifiedOrderDto?>("Sipariş bulunamadı.", false, null, HttpStatusCode.NotFound));
        }

        return ApiResponse(ApiResult("Sipariş detayı alındı.", true, detail));
    }
}
