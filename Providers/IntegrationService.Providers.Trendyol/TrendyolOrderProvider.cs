using IntegrationService.Core.Contracts;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Models.Invoices;
using IntegrationService.Core.Models.Orders;
using IntegrationService.Core.Providers;
using IntegrationService.Providers.Trendyol.Models.GetOrders;
using RestSharp;
using RestSharp.Serializers.Json;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace IntegrationService.Providers.Trendyol;

public sealed class TrendyolOrderProvider : IOrderProvider, IInvoiceProvider
{
    private const string OrdersRelativePathFormat = "order/sellers/{0}/orders";

    private readonly TrendyolCredentials _credentials;
    private readonly RestClient _restClient;

    public TrendyolOrderProvider(TrendyolCredentials credentials)
    {
        _credentials = credentials;
        _credentials.Validate();

        var basicToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_credentials.ApiKey}:{_credentials.ApiSecret}"));
        _restClient = new RestClient(
            new Uri("https://stageapigw.trendyol.com/integration/", UriKind.Absolute),
            configureDefaultHeaders: headers =>
            {
                headers.TryAddWithoutValidation("Accept", "application/json");
                headers.TryAddWithoutValidation("Authorization", $"Basic {basicToken}");
                headers.TryAddWithoutValidation("User-Agent", $"{_credentials.SupplierId} - SelfIntegration");
            },
            configureSerialization: cfg =>
                cfg.UseSystemTextJson(TrendyolOrderMapper.SerializerOptions));
    }

    public IntegrationProviderType Provider => IntegrationProviderType.Trendyol;

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
        var restRequest = new RestRequest(path);

        var restResponse = await _restClient.ExecuteAsync<TrendyolOrdersPageResponse>(restRequest, cancellationToken);
        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Trendyol API kimlik doğrulaması başarısız.");
        if (!restResponse.IsSuccessful)
        {
            var body = restResponse.Content ?? string.Empty;
            var msg = string.IsNullOrWhiteSpace(body)
                ? $"Trendyol API {(int)restResponse.StatusCode}"
                : $"Trendyol API hatası: {(int)restResponse.StatusCode}";
            throw new ExternalProviderException(msg, restResponse.StatusCode);
        }

        var payload = restResponse.Data ?? new TrendyolOrdersPageResponse();
        return TrendyolOrderMapper.ToPageDto(payload);
    }

    public async Task<UnifiedOrderDto?> GetOrderDetailAsync(string externalOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalOrderId))
            return null;

        var qs = new List<string> { "page=0", "size=50" };
        if (long.TryParse(externalOrderId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var packageId))
            qs.Add($"shipmentPackageIds={packageId}");
        else
            qs.Add($"orderNumber={Uri.EscapeDataString(externalOrderId)}");
        var path = string.Format(OrdersRelativePathFormat, _credentials.SupplierId) + "?" + string.Join('&', qs);
        var restRequest = new RestRequest(path);

        var restResponse = await _restClient.ExecuteAsync<TrendyolOrdersPageResponse>(restRequest, cancellationToken);
        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Trendyol API kimlik doğrulaması başarısız.");
        if (!restResponse.IsSuccessful)
        {
            var body = restResponse.Content ?? string.Empty;
            var msg = string.IsNullOrWhiteSpace(body)
                ? $"Trendyol API {(int)restResponse.StatusCode}"
                : $"Trendyol API hatası: {(int)restResponse.StatusCode}";
            throw new ExternalProviderException(msg, restResponse.StatusCode);
        }

        var payload = restResponse.Data;
        var first = payload?.Content?.FirstOrDefault();
        return first is null ? null : TrendyolOrderMapper.ToDetail(first);
    }

    public async Task<InvoiceOperationResultDto> CreateInvoiceAsync(InvoiceCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.InvoiceUrl))
            throw new ProviderValidationException("Trendyol fatura linki (InvoiceUrl) zorunludur.");
        if (!long.TryParse(request.OrderId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var packageId))
            throw new ProviderValidationException("Trendyol için OrderId, shipmentPackageId (sayı) olmalıdır.");

        var path = $"sellers/{_credentials.SupplierId}/seller-invoice-links";
        var payload = new Dictionary<string, object?>
        {
            ["invoiceLink"] = request.InvoiceUrl,
            ["shipmentPackageId"] = packageId,
            ["invoiceDateTime"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        if (!string.IsNullOrWhiteSpace(request.ExternalInvoiceNumber))
        {
            var s = request.ExternalInvoiceNumber.Trim();
            if (s.Length == 16
                && char.IsLetterOrDigit(s[0]) && char.IsLetterOrDigit(s[1]) && char.IsLetterOrDigit(s[2]))
            {
                var ok = true;
                for (var i = 3; i < 16 && ok; i++)
                {
                    if (!char.IsDigit(s[i]))
                        ok = false;
                }
                if (ok)
                    payload["invoiceNumber"] = s;
            }
        }

        var restRequest = new RestRequest(path, Method.Post);
        restRequest.AddStringBody(JsonSerializer.Serialize(payload), ContentType.Json);

        var restResponse = await _restClient.ExecuteAsync(restRequest, cancellationToken);
        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Trendyol API kimlik doğrulaması başarısız.");
        if (restResponse.StatusCode == HttpStatusCode.Conflict)
        {
            var body = restResponse.Content ?? string.Empty;
            throw new ExternalProviderException(string.IsNullOrWhiteSpace(body)
                ? "Trendyol: Bu paket için fatura zaten iletilmiş veya çakışma oluştu."
                : body, restResponse.StatusCode);
        }
        if (!restResponse.IsSuccessful)
        {
            var errBody = restResponse.Content ?? string.Empty;
            var msg = string.IsNullOrWhiteSpace(errBody)
                ? $"Trendyol API {(int)restResponse.StatusCode}"
                : $"Trendyol API hatası: {(int)restResponse.StatusCode}";
            throw new ExternalProviderException(msg, restResponse.StatusCode);
        }

        return new InvoiceOperationResultDto
        {
            Success = true,
            ProviderMessage = "Trendyol fatura linki kaydedildi.",
            ExternalInvoiceId = request.ExternalInvoiceNumber,
            DocumentUrl = request.InvoiceUrl
        };
    }
}
