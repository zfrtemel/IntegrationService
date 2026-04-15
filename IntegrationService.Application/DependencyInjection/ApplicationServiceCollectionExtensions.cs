using IntegrationService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrderApplicationService, OrderApplicationService>();
        services.AddScoped<IInvoiceApplicationService, InvoiceApplicationService>();
        return services;
    }
}
