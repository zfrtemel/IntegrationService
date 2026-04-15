using IntegrationService.Application.Abstractions;
using IntegrationService.Core.Contracts;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Providers;
using Microsoft.AspNetCore.Http;

namespace IntegrationService.Infrastructure.Providers;

public sealed class OrderProviderResolver : IProviderResolver
{
    private readonly IEnumerable<IOrderProviderFactory> _factories;

    public OrderProviderResolver(IEnumerable<IOrderProviderFactory> factories)
    {
        _factories = factories;
    }

    public ProviderResolution Resolve(HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue(IntegrationHeaderNames.Provider, out var providerRaw)
            || string.IsNullOrWhiteSpace(providerRaw))
        {
            throw new ProviderValidationException($"{IntegrationHeaderNames.Provider} header'ı zorunludur.");
        }

        if (!Enum.TryParse<IntegrationProviderType>(providerRaw.ToString(), ignoreCase: true, out var providerType))
        {
            throw new ProviderNotSupportedException($"Desteklenmeyen provider: '{providerRaw}'.");
        }

        var factory = _factories.FirstOrDefault(f => f.CanHandle(providerType));
        if (factory is null)
            throw new ProviderNotSupportedException($"Provider çözümlenemedi: {providerType}.");

        var resolution = factory.Create(httpContext);

        if (resolution.OrderProvider is not IInvoiceProvider)
            throw new ProviderNotSupportedException($"{providerType} provider'ı fatura sözleşmesini desteklemiyor.");

        return resolution;
    }
}
