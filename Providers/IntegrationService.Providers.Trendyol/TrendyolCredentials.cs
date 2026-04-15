using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Providers;

namespace IntegrationService.Providers.Trendyol;

public sealed record TrendyolCredentials(
    long SupplierId,
    string ApiKey,
    string ApiSecret)
{
    public void Validate()
    {
        if (SupplierId <= 0)
            throw new ProviderValidationException("Trendyol SupplierId geçerli bir pozitif sayı olmalıdır.");
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ProviderValidationException($"Trendyol API anahtarı ({IntegrationHeaderNames.ApiKey}) zorunludur.");
        if (string.IsNullOrWhiteSpace(ApiSecret))
            throw new ProviderValidationException($"Trendyol API secret ({IntegrationHeaderNames.ApiSecret}) zorunludur.");
    }
}
