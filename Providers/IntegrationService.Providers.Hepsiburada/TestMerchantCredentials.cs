using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Providers;

namespace IntegrationService.Providers.Hepsiburada;

public sealed record HepsiburadaCredentials(string MerchantId, string? Username, string? Password)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(MerchantId))
            throw new ProviderValidationException($"Hepsiburada için {IntegrationHeaderNames.MerchantId} header'ı zorunludur.");
    }
}
