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
        var username = IntegrationHeaderReader.Optional(httpContext, IntegrationHeaderNames.Username) ?? merchantId;
        var password = IntegrationHeaderReader.Required(httpContext, IntegrationHeaderNames.Password);

        var credentials = new HepsiburadaCredentials(merchantId, username, password);
        var provider = new HepsiburadaOrderProvider(credentials);
        return new ProviderResolution(provider, provider);
    }
}
