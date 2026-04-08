using IntegrationService.Core.Contracts;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Models.Providers;
using IntegrationService.Core.Providers;
using IntegrationService.Providers.Trendyol;
using HepsiburadaProvider = IntegrationService.Providers.Hepsiburada;
using N11Provider = IntegrationService.Providers.N11;
using PttavmProvider = IntegrationService.Providers.Pttavm;

namespace IntegrationService.Api.Infrastructure;

public interface IOrderProviderFactory
{
    ProviderResolution Resolve(HttpContext httpContext);
}

public sealed class OrderProviderFactory : IOrderProviderFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OrderProviderFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public ProviderResolution Resolve(HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue(IntegrationHeaderNames.Provider, out var providerRaw)
            || string.IsNullOrWhiteSpace(providerRaw))
        {
            throw new ProviderValidationException($"{IntegrationHeaderNames.Provider} header'ı zorunludur.");
        }

        if (!Enum.TryParse<IntegrationProviderType>(providerRaw.ToString(), ignoreCase: true, out var providerType))
        {
            throw new ProviderNotSupportedException($"Desteklenmeyen provider: '{providerRaw}'.");
        }

        var orderProvider = providerType switch
        {
            IntegrationProviderType.Trendyol => CreateTrendyol(httpContext),
            IntegrationProviderType.Hepsiburada => CreateHepsiburada(httpContext),
            IntegrationProviderType.N11 => CreateN11(httpContext),
            IntegrationProviderType.Pttavm => CreatePttavm(httpContext),
            _ => throw new ProviderNotSupportedException($"Provider çözümlenemedi: {providerType}.")
        };

        if (orderProvider is not IInvoiceProvider invoiceProvider)
            throw new ProviderNotSupportedException($"{providerType} provider'ı fatura sözleşmesini desteklemiyor.");

        return new ProviderResolution(orderProvider, invoiceProvider, orderProvider.Capabilities);
    }

    private IOrderProvider CreateTrendyol(HttpContext httpContext)
    {
        var supplierId = ReadRequiredHeader(httpContext, IntegrationHeaderNames.TrendyolSupplierId);
        if (!long.TryParse(supplierId, out var supplierIdValue))
            throw new ProviderValidationException("X-Trendyol-Supplier-Id geçerli bir sayı olmalıdır.");

        var apiKey = ReadRequiredHeader(httpContext, IntegrationHeaderNames.TrendyolApiKey);
        var apiSecret = ReadRequiredHeader(httpContext, IntegrationHeaderNames.TrendyolApiSecret);

        var storeFront = ReadOptionalHeader(httpContext, IntegrationHeaderNames.TrendyolStoreFrontCode) ?? "TR";
        var label = ReadOptionalHeader(httpContext, IntegrationHeaderNames.TrendyolIntegrationLabel) ?? "SelfIntegration";

        var credentials = new TrendyolCredentials(supplierIdValue, apiKey, apiSecret, storeFront, label);
        var http = _httpClientFactory.CreateClient("Trendyol");
        return new TrendyolOrderProvider(credentials, http);
    }

    private IOrderProvider CreateHepsiburada(HttpContext httpContext)
    {
        var merchantId = ReadRequiredHeader(httpContext, IntegrationHeaderNames.HepsiburadaMerchantId);
        var username = ReadOptionalHeader(httpContext, IntegrationHeaderNames.HepsiburadaUsername);
        var password = ReadOptionalHeader(httpContext, IntegrationHeaderNames.HepsiburadaPassword);
        var credentials = new HepsiburadaProvider.HepsiburadaCredentials(merchantId, username, password);
        var http = _httpClientFactory.CreateClient("Hepsiburada");
        return new HepsiburadaProvider.HepsiburadaTestOrderProvider(credentials, http);
    }

    private IOrderProvider CreateN11(HttpContext httpContext)
    {
        var apiKey = ReadRequiredHeader(httpContext, IntegrationHeaderNames.N11ApiKey);
        var apiSecret = ReadRequiredHeader(httpContext, IntegrationHeaderNames.N11ApiSecret);
        var merchantId = ReadOptionalHeader(httpContext, IntegrationHeaderNames.N11MerchantId) ?? "n11-default";
        var credentials = new N11Provider.N11Credentials(apiKey, apiSecret, merchantId);
        var http = _httpClientFactory.CreateClient("N11");
        return new N11Provider.N11TestOrderProvider(credentials, http);
    }

    private static IOrderProvider CreatePttavm(HttpContext httpContext)
    {
        var apiKey = ReadRequiredHeader(httpContext, IntegrationHeaderNames.PttavmApiKey);
        var accessToken = ReadRequiredHeader(httpContext, IntegrationHeaderNames.PttavmAccessToken);
        var correlationId = ReadOptionalHeader(httpContext, IntegrationHeaderNames.PttavmCorrelationId) ?? Guid.NewGuid().ToString("N");
        var credentials = new PttavmProvider.PttavmCredentials(apiKey, accessToken, correlationId);
        return new PttavmProvider.PttavmOrderProvider(credentials);
    }

    private static string ReadRequiredHeader(HttpContext httpContext, string headerName)
    {
        if (!httpContext.Request.Headers.TryGetValue(headerName, out var value) || string.IsNullOrWhiteSpace(value))
            throw new ProviderValidationException($"{headerName} header'ı zorunludur.");
        return value.ToString();
    }

    private static string? ReadOptionalHeader(HttpContext httpContext, string headerName)
    {
        return httpContext.Request.Headers.TryGetValue(headerName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : null;
    }
}

public sealed record ProviderResolution(
    IOrderProvider OrderProvider,
    IInvoiceProvider InvoiceProvider,
    ProviderCapabilities Capabilities);
