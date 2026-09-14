using IntegrationService.Core.Contracts;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Models.Invoices;
using IntegrationService.Core.Models.Orders;
using IntegrationService.Core.Providers;
using IntegrationService.Providers.Shopify.Models.GetOrders;
using RestSharp;
using RestSharp.Serializers.Json;
using System.Globalization;
using System.Net;

namespace IntegrationService.Providers.Shopify;

public sealed class ShopifyOrderProvider : IOrderProvider, IInvoiceProvider
{
    private const string OrdersRelativePath = "orders.json";
    private const string OrdersCountRelativePath = "orders/count.json";
    private const int MaxPageSize = 250;

    private readonly ShopifyCredentials _credentials;
    private readonly RestClient _restClient;

    public ShopifyOrderProvider(ShopifyCredentials credentials)
    {
        _credentials = credentials;
        _credentials.Validate();

        _restClient = new RestClient(
            _credentials.BaseUri,
            configureDefaultHeaders: headers =>
            {
                headers.TryAddWithoutValidation("Accept", "application/json");
                headers.TryAddWithoutValidation("X-Shopify-Access-Token", _credentials.AccessToken);
            },
            configureSerialization: cfg =>
                cfg.UseSystemTextJson(ShopifyOrderMapper.SerializerOptions));
    }

    public IntegrationProviderType Provider => IntegrationProviderType.Shopify;

    public async Task<UnifiedOrderPageDto> GetOrdersAsync(UnifiedOrderQuery query, CancellationToken cancellationToken = default)
    {
        var size = Math.Clamp(query.Size, 1, MaxPageSize);
        var page = Math.Max(query.Page, 0);

        // Sipariş numarası ile arama Shopify'da ayrı bir uçtan yürür.
        if (!string.IsNullOrWhiteSpace(query.OrderNumber))
        {
            var match = await FindByOrderNameAsync(query.OrderNumber!, cancellationToken);
            var items = match is null ? Array.Empty<UnifiedOrderDto>() : [match];
            return new UnifiedOrderPageDto
            {
                Items = items,
                Page = 0,
                Size = size,
                TotalPages = match is null ? 0 : 1,
                TotalElements = match is null ? 0 : 1
            };
        }

        var filters = BuildFilters(query);

        // Shopify REST sayfalama cursor tabanlıdır; page/size sözleşmesini korumak için
        // istenen sayfaya kadar since_id ile ilerliyoruz.
        var sinceId = page == 0 ? null : await SeekSinceIdAsync(filters, page, size, cancellationToken);
        if (page > 0 && sinceId is null)
        {
            var emptyTotal = await GetCountAsync(filters, cancellationToken);
            return ShopifyOrderMapper.ToPageDto(new ShopifyOrdersResponse(), page, size, emptyTotal);
        }

        var qs = new List<string>(filters) { $"limit={size}" };
        if (sinceId is not null)
            qs.Add($"since_id={sinceId}");

        var response = await ExecuteOrdersAsync(qs, cancellationToken);
        var total = await GetCountAsync(filters, cancellationToken);

        return ShopifyOrderMapper.ToPageDto(response, page, size, total);
    }

    public async Task<UnifiedOrderDto?> GetOrderDetailAsync(string externalOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalOrderId))
            return null;

        // Sayısal değer Shopify order id'sidir; aksi halde "#1001" gibi sipariş adıdır.
        if (!long.TryParse(externalOrderId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var orderId))
            return await FindByOrderNameAsync(externalOrderId, cancellationToken);

        var restRequest = new RestRequest($"orders/{orderId}.json");
        var restResponse = await _restClient.ExecuteAsync<ShopifyOrderResponse>(restRequest, cancellationToken);

        if (restResponse.StatusCode == HttpStatusCode.NotFound)
            return null;

        EnsureSuccessful(restResponse);

        var order = restResponse.Data?.Order;
        return order is null ? null : ShopifyOrderMapper.ToDetail(order);
    }

    /// <summary>
    /// Shopify'da pazaryerlerindeki gibi satıcı faturası yükleme ucu bulunmaz;
    /// fatura akışı mağazanın kendi uygulamaları üzerinden yürür.
    /// </summary>
    public Task<InvoiceOperationResultDto> CreateInvoiceAsync(InvoiceCreateRequestDto request, CancellationToken cancellationToken = default)
        => throw new NotSupportedByProviderException("Shopify fatura oluşturma işlemini desteklemiyor.");

    private async Task<UnifiedOrderDto?> FindByOrderNameAsync(string orderName, CancellationToken cancellationToken)
    {
        var qs = new List<string>
        {
            "status=any",
            "limit=1",
            $"name={Uri.EscapeDataString(orderName.Trim())}"
        };

        var response = await ExecuteOrdersAsync(qs, cancellationToken);
        var order = response.Orders?.FirstOrDefault();
        return order is null ? null : ShopifyOrderMapper.ToDetail(order);
    }

    private static List<string> BuildFilters(UnifiedOrderQuery query)
    {
        var filters = new List<string>();

        if (query.StartDate.HasValue)
            filters.Add($"created_at_min={Uri.EscapeDataString(query.StartDate.Value.ToString("o", CultureInfo.InvariantCulture))}");
        if (query.EndDate.HasValue)
            filters.Add($"created_at_max={Uri.EscapeDataString(query.EndDate.Value.ToString("o", CultureInfo.InvariantCulture))}");

        // Shopify varsayılanı yalnızca açık siparişlerdir; statü verilmediğinde tümünü getiriyoruz.
        filters.Add(string.IsNullOrWhiteSpace(query.Status)
            ? "status=any"
            : $"status={Uri.EscapeDataString(query.Status.Trim())}");

        return filters;
    }

    /// <summary>Hedef sayfanın başlangıcındaki since_id değerini bulur; sayfa aralık dışındaysa null döner.</summary>
    private async Task<long?> SeekSinceIdAsync(List<string> filters, int page, int size, CancellationToken cancellationToken)
    {
        long? sinceId = null;

        for (var i = 0; i < page; i++)
        {
            var qs = new List<string>(filters)
            {
                $"limit={size}",
                "fields=id",
                "order=id+asc"
            };
            if (sinceId is not null)
                qs.Add($"since_id={sinceId}");

            var response = await ExecuteOrdersAsync(qs, cancellationToken);
            var lastId = response.Orders?.LastOrDefault()?.Id;

            // Bu sayfa dolmadıysa sonrasında kayıt kalmamıştır.
            if (lastId is null || (response.Orders?.Count ?? 0) < size)
                return null;

            sinceId = lastId;
        }

        return sinceId;
    }

    private async Task<int?> GetCountAsync(List<string> filters, CancellationToken cancellationToken)
    {
        var path = OrdersCountRelativePath + "?" + string.Join('&', filters);
        var restResponse = await _restClient.ExecuteAsync<ShopifyOrderCountResponse>(new RestRequest(path), cancellationToken);

        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Shopify API kimlik doğrulaması başarısız.");

        // Toplam sayı yardımcı bilgidir; alınamazsa listeleme yine de sonuç dönmelidir.
        return restResponse.IsSuccessful ? restResponse.Data?.Count : null;
    }

    private async Task<ShopifyOrdersResponse> ExecuteOrdersAsync(List<string> queryString, CancellationToken cancellationToken)
    {
        var path = OrdersRelativePath + "?" + string.Join('&', queryString);
        var restResponse = await _restClient.ExecuteAsync<ShopifyOrdersResponse>(new RestRequest(path), cancellationToken);

        EnsureSuccessful(restResponse);
        return restResponse.Data ?? new ShopifyOrdersResponse();
    }

    private static void EnsureSuccessful(RestResponse restResponse)
    {
        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Shopify API kimlik doğrulaması başarısız.");

        if (restResponse.StatusCode == HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException("Shopify access token'ı 'read_orders' yetkisine sahip değil.");

        if (!restResponse.IsSuccessful)
        {
            var msg = $"Shopify API hatası: {(int)restResponse.StatusCode}";
            throw new ExternalProviderException(msg, restResponse.StatusCode);
        }
    }
}
