using IntegrationService.Application.Abstractions;
using IntegrationService.Core.Providers;
using IntegrationService.Providers.Shopify;
using Microsoft.AspNetCore.Http;

namespace IntegrationService.Infrastructure.Providers.Factories;

public sealed class ShopifyOrderProviderFactory : IOrderProviderFactory
{
    public bool CanHandle(IntegrationProviderType type) => type == IntegrationProviderType.Shopify;

    public ProviderResolution Create(HttpContext httpContext)
    {
        var shopDomain = IntegrationHeaderReader.Required(httpContext, IntegrationHeaderNames.ShopDomain);
        var accessToken = IntegrationHeaderReader.Required(httpContext, IntegrationHeaderNames.AccessToken);
        var apiVersion = IntegrationHeaderReader.Optional(httpContext, IntegrationHeaderNames.ApiVersion);

        var credentials = new ShopifyCredentials(
            shopDomain,
            accessToken,
            string.IsNullOrWhiteSpace(apiVersion) ? ShopifyCredentials.DefaultApiVersion : apiVersion);

        var provider = new ShopifyOrderProvider(credentials);
        return new ProviderResolution(provider, provider);
    }
}
