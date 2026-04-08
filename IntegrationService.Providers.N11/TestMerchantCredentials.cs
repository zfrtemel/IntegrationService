using IntegrationService.Core.Exceptions;

namespace IntegrationService.Providers.N11;

public sealed record N11Credentials(string ApiKey, string ApiSecret, string MerchantId)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ProviderValidationException("N11 için X-N11-Api-Key zorunludur.");
        if (string.IsNullOrWhiteSpace(ApiSecret))
            throw new ProviderValidationException("N11 için X-N11-Api-Secret zorunludur.");
    }
}
