using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Providers;

namespace IntegrationService.Providers.Pttavm;

public sealed record PttavmCredentials(string ApiKey, string AccessToken, string CorrelationId)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ProviderValidationException($"PttAVM için {IntegrationHeaderNames.ApiKey} zorunludur.");
        if (string.IsNullOrWhiteSpace(AccessToken))
            throw new ProviderValidationException($"PttAVM için {IntegrationHeaderNames.AccessToken} zorunludur.");
        if (string.IsNullOrWhiteSpace(CorrelationId))
            throw new ProviderValidationException($"PttAVM için {IntegrationHeaderNames.CorrelationId} zorunludur.");
    }
}
