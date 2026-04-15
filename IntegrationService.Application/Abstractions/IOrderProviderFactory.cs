using IntegrationService.Core.Providers;
using Microsoft.AspNetCore.Http;

namespace IntegrationService.Application.Abstractions;

public interface IOrderProviderFactory
{
    bool CanHandle(IntegrationProviderType type);

    ProviderResolution Create(HttpContext httpContext);
}
