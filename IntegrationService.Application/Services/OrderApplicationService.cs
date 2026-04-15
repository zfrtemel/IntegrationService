using IntegrationService.Application.Abstractions;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Models.Orders;
using Microsoft.AspNetCore.Http;

namespace IntegrationService.Application.Services;

public interface IOrderApplicationService
{
    Task<UnifiedOrderPageDto> ListAsync(HttpContext httpContext, UnifiedOrderQuery query, CancellationToken cancellationToken = default);
    Task<UnifiedOrderDto?> DetailAsync(HttpContext httpContext, string externalOrderId, CancellationToken cancellationToken = default);
}

public sealed class OrderApplicationService : IOrderApplicationService
{
    private readonly IProviderResolver _providerResolver;

    public OrderApplicationService(IProviderResolver providerResolver)
    {
        _providerResolver = providerResolver;
    }

    public async Task<UnifiedOrderPageDto> ListAsync(HttpContext httpContext, UnifiedOrderQuery query, CancellationToken cancellationToken = default)
    {
        var resolved = _providerResolver.Resolve(httpContext);
        return await resolved.OrderProvider.GetOrdersAsync(query, cancellationToken);
    }

    public async Task<UnifiedOrderDto?> DetailAsync(HttpContext httpContext, string externalOrderId, CancellationToken cancellationToken = default)
    {
        var resolved = _providerResolver.Resolve(httpContext);
        var responseDetail = await resolved.OrderProvider.GetOrderDetailAsync(externalOrderId, cancellationToken);

        if (responseDetail is null)
            throw new NotFoundException("Sipariş bulunamadı.");

        return responseDetail;
    }
}
