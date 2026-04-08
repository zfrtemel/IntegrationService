using IntegrationService.Core.Exceptions;

namespace IntegrationService.Providers.Trendyol;

public sealed record TrendyolCredentials(
    long SupplierId,
    string ApiKey,
    string ApiSecret,
    string StoreFrontCode,
    string IntegrationLabel)
{
    public void Validate()
    {
        if (SupplierId <= 0)
            throw new ProviderValidationException("Trendyol SupplierId geçerli bir pozitif sayı olmalıdır.");
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ProviderValidationException("Trendyol API anahtarı (X-Trendyol-Api-Key) zorunludur.");
        if (string.IsNullOrWhiteSpace(ApiSecret))
            throw new ProviderValidationException("Trendyol API secret (X-Trendyol-Api-Secret) zorunludur.");
        if (string.IsNullOrWhiteSpace(StoreFrontCode))
            throw new ProviderValidationException("Trendyol mağaza kodu (X-Trendyol-Store-Front-Code) zorunludur.");
        if (string.IsNullOrWhiteSpace(IntegrationLabel))
            throw new ProviderValidationException("Trendyol entegrasyon etiketi (X-Trendyol-Integration-Label) boş olamaz.");
    }
}
