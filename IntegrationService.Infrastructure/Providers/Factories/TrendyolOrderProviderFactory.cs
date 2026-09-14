using IntegrationService.Application.Abstractions;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Providers;
using IntegrationService.Providers.Trendyol;
using Microsoft.AspNetCore.Http;

namespace IntegrationService.Infrastructure.Providers.Factories;

public sealed class TrendyolOrderProviderFactory : IOrderProviderFactory
{
    public bool CanHandle(IntegrationProviderType type) => type == IntegrationProviderType.Trendyol;

    public ProviderResolution Create(HttpContext httpContext)
    {
        var supplierIdRaw = IntegrationHeaderReader.Required(httpContext, IntegrationHeaderNames.SupplierId);
        if (!long.TryParse(supplierIdRaw, out var supplierId))
            throw new ProviderValidationException($"{IntegrationHeaderNames.SupplierId} geçerli bir sayı olmalıdır.");

        var apiKey = IntegrationHeaderReader.Required(httpContext, IntegrationHeaderNames.ApiKey);
        var apiSecret = IntegrationHeaderReader.Required(httpContext, IntegrationHeaderNames.ApiSecret);

        var credentials = new TrendyolCredentials(supplierId, apiKey, apiSecret);
        var provider = new TrendyolOrderProvider(credentials);
        return new ProviderResolution(provider, provider);
    }
}
