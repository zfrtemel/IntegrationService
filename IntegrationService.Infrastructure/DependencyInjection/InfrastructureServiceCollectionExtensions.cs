using IntegrationService.Application.Abstractions;
using IntegrationService.Infrastructure.Providers;
using IntegrationService.Infrastructure.Providers.Factories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IOrderProviderFactory, TrendyolOrderProviderFactory>();
        services.AddScoped<IOrderProviderFactory, HepsiburadaOrderProviderFactory>();
        services.AddScoped<IOrderProviderFactory, N11OrderProviderFactory>();
        services.AddScoped<IOrderProviderFactory, PttavmOrderProviderFactory>();
        services.AddScoped<IOrderProviderFactory, ShopifyOrderProviderFactory>();

        services.AddScoped<IProviderResolver, OrderProviderResolver>();

        return services;
    }
}
