using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Providers;

namespace IntegrationService.Providers.N11;

public sealed record N11Credentials(string ApiKey, string ApiSecret)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ProviderValidationException($"N11 için {IntegrationHeaderNames.ApiKey} zorunludur.");
        if (string.IsNullOrWhiteSpace(ApiSecret))
            throw new ProviderValidationException($"N11 için {IntegrationHeaderNames.ApiSecret} zorunludur.");
    }
}
