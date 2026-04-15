using IntegrationService.Application.Abstractions;
using IntegrationService.Core.Providers;
using IntegrationService.Providers.Hepsiburada;
using Microsoft.AspNetCore.Http;

namespace IntegrationService.Infrastructure.Providers.Factories;

public sealed class HepsiburadaOrderProviderFactory : IOrderProviderFactory
{

    public bool CanHandle(IntegrationProviderType type) => type == IntegrationProviderType.Hepsiburada;

    public ProviderResolution Create(HttpContext httpContext)
    {
        var merchantId = IntegrationHeaderReader.Required(httpContext, IntegrationHeaderNames.MerchantId);
        var username = IntegrationHeaderReader.Optional(httpContext, IntegrationHeaderNames.Username);
        var password = IntegrationHeaderReader.Optional(httpContext, IntegrationHeaderNames.Password);
        var credentials = new HepsiburadaCredentials("e27408f6-9575-464f-bdf4-17356b2b56a6", "e27408f6-9575-464f-bdf4-17356b2b56a6", "9Zn17aNkdvxD");
        var provider = new HepsiburadaOrderProvider(credentials);
        return new ProviderResolution(provider, provider);
    }
}
