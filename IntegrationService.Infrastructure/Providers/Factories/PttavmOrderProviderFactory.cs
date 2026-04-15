using IntegrationService.Application.Abstractions;
using IntegrationService.Core.Providers;
using IntegrationService.Providers.Pttavm;
using Microsoft.AspNetCore.Http;

namespace IntegrationService.Infrastructure.Providers.Factories;

public sealed class PttavmOrderProviderFactory : IOrderProviderFactory
{
    public bool CanHandle(IntegrationProviderType type) => type == IntegrationProviderType.Pttavm;

    public ProviderResolution Create(HttpContext httpContext)
    {
        var apiKey = IntegrationHeaderReader.Required(httpContext, IntegrationHeaderNames.ApiKey);
        var accessToken = IntegrationHeaderReader.Required(httpContext, IntegrationHeaderNames.AccessToken);
        var correlationId = IntegrationHeaderReader.Optional(httpContext, IntegrationHeaderNames.CorrelationId) ?? Guid.NewGuid().ToString("N");
        var credentials = new PttavmCredentials(apiKey, accessToken, correlationId);
        var provider = new PttavmOrderProvider(credentials);
        return new ProviderResolution(provider, provider);
    }
}
