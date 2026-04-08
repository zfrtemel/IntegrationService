using IntegrationService.Core.Exceptions;

namespace IntegrationService.Providers.Hepsiburada;

public sealed record HepsiburadaCredentials(string MerchantId, string? Username, string? Password)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(MerchantId))
            throw new ProviderValidationException("Hepsiburada için X-Hb-Merchant-Id header'ı zorunludur.");
    }
}
