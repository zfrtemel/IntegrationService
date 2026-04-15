using IntegrationService.Application.Abstractions;
using IntegrationService.Core.Providers;
using IntegrationService.Providers.N11;
using Microsoft.AspNetCore.Http;

namespace IntegrationService.Infrastructure.Providers.Factories;

public sealed class N11OrderProviderFactory : IOrderProviderFactory
{
    public bool CanHandle(IntegrationProviderType type) => type == IntegrationProviderType.N11;

    public ProviderResolution Create(HttpContext httpContext)
    {
        var apiKey = IntegrationHeaderReader.Required(httpContext, IntegrationHeaderNames.ApiKey);
        var apiSecret = IntegrationHeaderReader.Required(httpContext, IntegrationHeaderNames.ApiSecret);
        var credentials = new N11Credentials("030a5d62-0eb1-4c19-9a44-8dc3bb674b25", "TA5OX7OAFq5pP5Rr");
        var provider = new N11OrderProvider(credentials);
        return new ProviderResolution(provider, provider);
    }
}
