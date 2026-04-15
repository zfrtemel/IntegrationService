using IntegrationService.Core.Contracts;
using Microsoft.AspNetCore.Http;

namespace IntegrationService.Application.Abstractions;

public interface IProviderResolver
{
    ProviderResolution Resolve(HttpContext httpContext);
}

public sealed record ProviderResolution(
    IOrderProvider OrderProvider,
    IInvoiceProvider InvoiceProvider);
