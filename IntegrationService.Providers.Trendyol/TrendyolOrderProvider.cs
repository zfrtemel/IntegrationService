using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using IntegrationService.Core.Contracts;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Models.Invoices;
using IntegrationService.Core.Models.Orders;
using IntegrationService.Core.Models.Providers;
using IntegrationService.Core.Providers;

namespace IntegrationService.Providers.Trendyol;

public sealed class TrendyolOrderProvider : IOrderProvider, IInvoiceProvider
{
    private const string OrdersRelativePathFormat = "order/sellers/{0}/orders";

    private readonly TrendyolCredentials _credentials;
    private readonly HttpClient _httpClient;

    public TrendyolOrderProvider(TrendyolCredentials credentials, HttpClient httpClient)
    {
        _credentials = credentials;
        _httpClient = httpClient;
        _credentials.Validate();
    }

    public IntegrationProviderType Provider => IntegrationProviderType.Trendyol;
    public ProviderCapabilities Capabilities => new()
    {
        SupportsOrderBillingFields = true,
        SupportsInvoiceCreate = false,
        SupportsInvoiceCancel = false,
        SupportsInvoiceDownload = false
    };

    public async Task<UnifiedOrderPageDto> GetOrdersAsync(UnifiedOrderQuery query, CancellationToken cancellationToken = default)
    {
        var qs = new List<string>
        {
            $"page={query.Page}",
            $"size={Math.Clamp(query.Size, 1, 200)}"
        };
        if (query.StartDateUnixMs.HasValue)
            qs.Add($"startDate={query.StartDateUnixMs.Value}");
        if (query.EndDateUnixMs.HasValue)
            qs.Add($"endDate={query.EndDateUnixMs.Value}");
        if (!string.IsNullOrWhiteSpace(query.Status))
            qs.Add($"status={Uri.EscapeDataString(query.Status)}");
        if (!string.IsNullOrWhiteSpace(query.OrderNumber))
            qs.Add($"orderNumber={Uri.EscapeDataString(query.OrderNumber)}");

        var path = string.Format(OrdersRelativePathFormat, _credentials.SupplierId) + "?" + string.Join('&', qs);
        using var request = CreateRequest(HttpMethod.Get, path);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccess(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<TrendyolOrdersPageResponse>(stream, TrendyolOrderMapper.SerializerOptions, cancellationToken)
            ?? new TrendyolOrdersPageResponse();

        return TrendyolOrderMapper.ToPageDto(payload);
    }

    public async Task<UnifiedOrderDto?> GetOrderDetailAsync(string externalOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalOrderId))
            return null;

        var qs = new List<string> { "page=0", "size=50" };
        if (long.TryParse(externalOrderId, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var packageId))
            qs.Add($"shipmentPackageIds={packageId}");
        else
            qs.Add($"orderNumber={Uri.EscapeDataString(externalOrderId)}");
        var path = string.Format(OrdersRelativePathFormat, _credentials.SupplierId) + "?" + string.Join('&', qs);
        using var request = CreateRequest(HttpMethod.Get, path);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccess(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<TrendyolOrdersPageResponse>(stream, TrendyolOrderMapper.SerializerOptions, cancellationToken);
        var first = payload?.Content?.FirstOrDefault();
        return first is null ? null : TrendyolOrderMapper.ToDetail(first);
    }

    public Task<InvoiceOperationResultDto> CreateInvoiceAsync(InvoiceCreateRequestDto request, CancellationToken cancellationToken = default)
        => throw new NotSupportedByProviderException("Trendyol fatura operasyonları bu adaptörde henüz tanımlanmadı.");

    public Task<InvoiceOperationResultDto> CancelInvoiceAsync(InvoiceCancelRequestDto request, CancellationToken cancellationToken = default)
        => throw new NotSupportedByProviderException("Trendyol fatura iptal operasyonu bu adaptörde desteklenmiyor.");

    public Task<InvoiceDocumentResultDto> GetInvoiceDocumentAsync(InvoiceDocumentRequestDto request, CancellationToken cancellationToken = default)
        => throw new NotSupportedByProviderException("Trendyol fatura dökümanı operasyonu bu adaptörde desteklenmiyor.");

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUri)
    {
        var request = new HttpRequestMessage(method, relativeUri);
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_credentials.ApiKey}:{_credentials.ApiSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        request.Headers.TryAddWithoutValidation("User-Agent", $"{_credentials.SupplierId} - {_credentials.IntegrationLabel}");
        request.Headers.TryAddWithoutValidation("storeFrontCode", _credentials.StoreFrontCode);
        return request;
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Trendyol API kimlik doğrulaması başarısız.");

        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var msg = string.IsNullOrWhiteSpace(body)
            ? $"Trendyol API {(int)response.StatusCode} {response.ReasonPhrase}"
            : $"Trendyol API hatası: {(int)response.StatusCode}";

        throw new ExternalProviderException(msg, response.StatusCode);
    }
}
