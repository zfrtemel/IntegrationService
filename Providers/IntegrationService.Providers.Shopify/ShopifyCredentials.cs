using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Providers;

namespace IntegrationService.Providers.Shopify;

/// <param name="ShopDomain">Mağaza alan adı, örn. "my-store.myshopify.com".</param>
/// <param name="AccessToken">Admin API access token ("shpat_" ile başlar), write_orders yetkisi gerekir.</param>
/// <param name="ApiVersion">Admin API sürümü, örn. "2025-07".</param>
public sealed record ShopifyCredentials(
    string ShopDomain,
    string AccessToken,
    string ApiVersion = ShopifyCredentials.DefaultApiVersion)
{
    public const string DefaultApiVersion = "2025-07";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ShopDomain))
            throw new ProviderValidationException($"Shopify mağaza alan adı ({IntegrationHeaderNames.ShopDomain}) zorunludur.");

        if (ShopDomain.Contains("://", StringComparison.Ordinal) || ShopDomain.Contains('/', StringComparison.Ordinal))
            throw new ProviderValidationException($"{IntegrationHeaderNames.ShopDomain} yalnızca alan adı olmalıdır, örn. 'my-store.myshopify.com'.");

        if (!ShopDomain.EndsWith(".myshopify.com", StringComparison.OrdinalIgnoreCase))
            throw new ProviderValidationException($"{IntegrationHeaderNames.ShopDomain} '.myshopify.com' ile bitmelidir.");

        if (string.IsNullOrWhiteSpace(AccessToken))
            throw new ProviderValidationException($"Shopify Admin API access token ({IntegrationHeaderNames.AccessToken}) zorunludur.");

        if (string.IsNullOrWhiteSpace(ApiVersion))
            throw new ProviderValidationException("Shopify API sürümü boş olamaz.");
    }

    public Uri BaseUri => new($"https://{ShopDomain}/admin/api/{ApiVersion}/", UriKind.Absolute);
}
