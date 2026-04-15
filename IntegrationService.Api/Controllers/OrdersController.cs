using IntegrationService.Application.Services;
using IntegrationService.Core.Models.Orders;
using IntegrationService.Core.Responses;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : BaseController
{
    private readonly IOrderApplicationService _orderApplicationService;

    public OrdersController(IOrderApplicationService orderApplicationService)
    {
        _orderApplicationService = orderApplicationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(APIResult<UnifiedOrderPageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] UnifiedOrderQuery filter, CancellationToken cancellationToken = default)
    {
        return ApiResult("Sipariş listesi alındı.", true, await _orderApplicationService.ListAsync(HttpContext, filter, cancellationToken));
    }

    [HttpGet("{externalOrderId}")]
    [ProducesResponseType(typeof(APIResult<UnifiedOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(APIResult<UnifiedOrderDto?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detail([FromRoute] string externalOrderId, CancellationToken cancellationToken = default)
    {
        var detail = await _orderApplicationService.DetailAsync(HttpContext, externalOrderId, cancellationToken);

        return ApiResult("Sipariş detayı alındı.", true, detail);
    }
}
