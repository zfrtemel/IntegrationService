using IntegrationService.Core.Models.Orders;
using IntegrationService.Core.Models.Providers;
using IntegrationService.Core.Providers;

namespace IntegrationService.Core.Contracts;

public interface IOrderProvider
{
    IntegrationProviderType Provider { get; }
    ProviderCapabilities Capabilities { get; }

    Task<UnifiedOrderPageDto> GetOrdersAsync(UnifiedOrderQuery query, CancellationToken cancellationToken = default);
    Task<UnifiedOrderDto?> GetOrderDetailAsync(string orderIdOrNumber, CancellationToken cancellationToken = default);
}
