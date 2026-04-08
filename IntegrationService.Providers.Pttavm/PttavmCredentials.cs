using IntegrationService.Core.Exceptions;

namespace IntegrationService.Providers.Pttavm;

public sealed record PttavmCredentials(string ApiKey, string AccessToken, string CorrelationId)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ProviderValidationException("PttAVM için X-Pttavm-Api-Key zorunludur.");
        if (string.IsNullOrWhiteSpace(AccessToken))
            throw new ProviderValidationException("PttAVM için X-Pttavm-Access-Token zorunludur.");
        if (string.IsNullOrWhiteSpace(CorrelationId))
            throw new ProviderValidationException("PttAVM için X-Pttavm-Correlation-Id zorunludur.");
    }
}
