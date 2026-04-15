using IntegrationService.Core.Contracts;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Models.Invoices;
using IntegrationService.Core.Models.Orders;
using IntegrationService.Core.Providers;
using IntegrationService.Providers.N11.Models.ShipmentPackages;
using N11SellerInvoiceService;
using RestSharp;
using RestSharp.Serializers.Json;
using System.Net;
using System.Text.Json;

namespace IntegrationService.Providers.N11;

public sealed class N11OrderProvider : IOrderProvider, IInvoiceProvider
{
    private readonly N11Credentials _credentials;
    private readonly RestClient _restClient;

    public N11OrderProvider(N11Credentials credentials)
    {
        _credentials = credentials;
        _credentials.Validate();

        _restClient = new RestClient(
            new Uri("https://api.n11.com/", UriKind.Absolute),
            configureDefaultHeaders: headers =>
            {
                headers.TryAddWithoutValidation("Accept", "application/json");
                headers.TryAddWithoutValidation("appkey", _credentials.ApiKey);
                headers.TryAddWithoutValidation("appsecret", _credentials.ApiSecret);
            },
            configureSerialization: cfg =>
                cfg.UseSystemTextJson(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));
    }

    public IntegrationProviderType Provider => IntegrationProviderType.N11;

    public async Task<UnifiedOrderPageDto> GetOrdersAsync(UnifiedOrderQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(0, query.Page);
        var size = Math.Clamp(query.Size <= 0 ? 50 : query.Size, 1, 100);
        var start = query.StartDateUnixMs ?? DateTimeOffset.UtcNow.AddDays(-2).ToUnixTimeMilliseconds();
        var end = query.EndDateUnixMs ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var queryParts = new List<string>
        {
            $"startDate={start}",
            $"endDate={end}",
            $"page={page}",
            $"size={size}",
            "orderByDirection=DESC"
        };

        if (!string.IsNullOrWhiteSpace(query.OrderNumber))
            queryParts.Add($"orderNumber={Uri.EscapeDataString(query.OrderNumber)}");
        if (!string.IsNullOrWhiteSpace(query.Status))
            queryParts.Add($"status={Uri.EscapeDataString(query.Status)}");

        var path = "rest/delivery/v1/shipmentPackages?" + string.Join("&", queryParts);

        var restRequest = new RestRequest(path);

        var restResponse = await _restClient.ExecuteAsync<N11ShipmentPackagesResponse>(restRequest, cancellationToken);
        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("N11 API kimlik doğrulaması başarısız.");
        if (!restResponse.IsSuccessful)
            throw new ExternalProviderException($"N11 shipmentPackages hatası: {(int)restResponse.StatusCode}", restResponse.StatusCode);

        var payload = restResponse.Data;

        var items = (payload?.Content ?? []).Select(package =>
        {
            var lines = (package.Lines ?? []).Select(line =>
            {
                var qty = line.Quantity ?? 0;
                var unit = line.Price ?? 0m;
                var fallbackTotal = (unit * qty) - (line.TotalSellerDiscountPrice ?? 0m);
                var total = line.SellerInvoiceAmount ?? fallbackTotal;
                return new UnifiedOrderLineDto
                {
                    LineId = line.OrderLineId?.ToString() ?? string.Empty,
                    Sku = line.StockCode ?? string.Empty,
                    Name = line.ProductName ?? string.Empty,
                    Qty = qty,
                    UnitPrice = unit,
                    LineTotal = total,
                    TaxRate = line.VatRate
                };
            }).ToList();

            var invoiceType = package.BillingAddress?.InvoiceType switch
            {
                1 => "Individual",
                2 => "Corporate",
                _ => "Unknown"
            };

            return new UnifiedOrderDto
            {
                OrderId = package.Id ?? string.Empty,
                OrderNumber = package.OrderNumber ?? string.Empty,
                Marketplace = IntegrationProviderType.N11,
                Status = package.ShipmentPackageStatus,
                CreatedAt = (package.LastModifiedDate.HasValue && package.LastModifiedDate.Value > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(package.LastModifiedDate.Value)
                    : (DateTimeOffset?)null)
                    ?? (package.AgreedDeliveryDate.HasValue && package.AgreedDeliveryDate.Value > 0
                        ? DateTimeOffset.FromUnixTimeMilliseconds(package.AgreedDeliveryDate.Value)
                        : (DateTimeOffset?)null),
                Customer = new OrderCustomerDto
                {
                    FullName = package.CustomerFullName ?? package.ShippingAddress?.FullName,
                    Email = package.CustomerEmail,
                    PhoneMasked = package.ShippingAddress?.Gsm
                },
                Amounts = new OrderAmountSummaryDto
                {
                    Subtotal = package.TotalAmount ?? 0m,
                    DiscountTotal = package.TotalDiscountAmount ?? 0m,
                    GrandTotal = package.TotalAmount ?? 0m,
                    Currency = "TRY"
                },
                ShippingAddress = new OrderAddressDto
                {
                    FullAddress = package.ShippingAddress?.Address,
                    City = package.ShippingAddress?.City,
                    District = package.ShippingAddress?.District,
                    PostalCode = package.ShippingAddress?.PostalCode,
                    CountryCode = "TR"
                },
                BillingInfo = new OrderBillingInfoDto
                {
                    InvoiceType = invoiceType,
                    BillingFullNameOrCompany = package.BillingAddress?.FullName ?? package.CustomerFullName,
                    TaxOffice = package.TaxOffice ?? package.BillingAddress?.TaxHouse,
                    TaxNumber = package.TaxId ?? package.BillingAddress?.TaxId,
                    Tckn = package.TcIdentityNumber ?? package.BillingAddress?.TcId,
                    InvoiceAvailable = package.BillingAddress is not null,
                    BillingAddress = new OrderAddressDto
                    {
                        FullAddress = package.BillingAddress?.Address,
                        City = package.BillingAddress?.City,
                        District = package.BillingAddress?.District,
                        PostalCode = package.BillingAddress?.PostalCode,
                        CountryCode = "TR"
                    }
                },
                Lines = lines
            };
        }).ToList();

        return new UnifiedOrderPageDto
        {
            Items = items,
            Page = payload?.Page ?? page,
            Size = payload?.Size ?? size,
            TotalPages = payload?.TotalPages ?? Math.Max(1, (int)Math.Ceiling(items.Count / (double)size)),
            TotalElements = payload?.Content?.Count ?? items.Count
        };
    }

    public async Task<UnifiedOrderDto?> GetOrderDetailAsync(string externalOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalOrderId))
            return null;

        var query = new UnifiedOrderQuery { Page = 0, Size = 50, OrderNumber = long.TryParse(externalOrderId, out _) ? null : externalOrderId };
        var packageIds = long.TryParse(externalOrderId, out _) ? externalOrderId : null;

        var page = Math.Max(0, query.Page);
        var size = Math.Clamp(query.Size <= 0 ? 50 : query.Size, 1, 100);
        var start = query.StartDateUnixMs ?? DateTimeOffset.UtcNow.AddDays(-2).ToUnixTimeMilliseconds();
        var end = query.EndDateUnixMs ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var queryParts = new List<string>
        {
            $"startDate={start}",
            $"endDate={end}",
            $"page={page}",
            $"size={size}",
            "orderByDirection=DESC"
        };

        if (!string.IsNullOrWhiteSpace(query.OrderNumber))
            queryParts.Add($"orderNumber={Uri.EscapeDataString(query.OrderNumber)}");
        if (!string.IsNullOrWhiteSpace(query.Status))
            queryParts.Add($"status={Uri.EscapeDataString(query.Status)}");

        var basePath = "rest/delivery/v1/shipmentPackages?" + string.Join("&", queryParts);
        var path = string.IsNullOrWhiteSpace(packageIds)
            ? basePath
            : basePath + $"&packageIds={Uri.EscapeDataString(packageIds)}";

        var restRequest = new RestRequest(path);

        var restResponse = await _restClient.ExecuteAsync<N11ShipmentPackagesResponse>(restRequest, cancellationToken);
        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("N11 API kimlik doğrulaması başarısız.");
        if (!restResponse.IsSuccessful)
            throw new ExternalProviderException($"N11 shipmentPackages hatası: {(int)restResponse.StatusCode}", restResponse.StatusCode);

        var payload = restResponse.Data;

        var items = (payload?.Content ?? []).Select(package =>
        {
            var lines = (package.Lines ?? []).Select(line =>
            {
                var qty = line.Quantity ?? 0;
                var unit = line.Price ?? 0m;
                var fallbackTotal = (unit * qty) - (line.TotalSellerDiscountPrice ?? 0m);
                var total = line.SellerInvoiceAmount ?? fallbackTotal;
                return new UnifiedOrderLineDto
                {
                    LineId = line.OrderLineId?.ToString() ?? string.Empty,
                    Sku = line.StockCode ?? string.Empty,
                    Name = line.ProductName ?? string.Empty,
                    Qty = qty,
                    UnitPrice = unit,
                    LineTotal = total,
                    TaxRate = line.VatRate
                };
            }).ToList();

            var invoiceType = package.BillingAddress?.InvoiceType switch
            {
                1 => "Individual",
                2 => "Corporate",
                _ => "Unknown"
            };

            return new UnifiedOrderDto
            {
                OrderId = package.Id ?? string.Empty,
                OrderNumber = package.OrderNumber ?? string.Empty,
                Marketplace = IntegrationProviderType.N11,
                Status = package.ShipmentPackageStatus,
                CreatedAt = (package.LastModifiedDate.HasValue && package.LastModifiedDate.Value > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(package.LastModifiedDate.Value)
                    : (DateTimeOffset?)null)
                    ?? (package.AgreedDeliveryDate.HasValue && package.AgreedDeliveryDate.Value > 0
                        ? DateTimeOffset.FromUnixTimeMilliseconds(package.AgreedDeliveryDate.Value)
                        : (DateTimeOffset?)null),
                Customer = new OrderCustomerDto
                {
                    FullName = package.CustomerFullName ?? package.ShippingAddress?.FullName,
                    Email = package.CustomerEmail,
                    PhoneMasked = package.ShippingAddress?.Gsm
                },
                Amounts = new OrderAmountSummaryDto
                {
                    Subtotal = package.TotalAmount ?? 0m,
                    DiscountTotal = package.TotalDiscountAmount ?? 0m,
                    GrandTotal = package.TotalAmount ?? 0m,
                    Currency = "TRY"
                },
                ShippingAddress = new OrderAddressDto
                {
                    FullAddress = package.ShippingAddress?.Address,
                    City = package.ShippingAddress?.City,
                    District = package.ShippingAddress?.District,
                    PostalCode = package.ShippingAddress?.PostalCode,
                    CountryCode = "TR"
                },
                BillingInfo = new OrderBillingInfoDto
                {
                    InvoiceType = invoiceType,
                    BillingFullNameOrCompany = package.BillingAddress?.FullName ?? package.CustomerFullName,
                    TaxOffice = package.TaxOffice ?? package.BillingAddress?.TaxHouse,
                    TaxNumber = package.TaxId ?? package.BillingAddress?.TaxId,
                    Tckn = package.TcIdentityNumber ?? package.BillingAddress?.TcId,
                    InvoiceAvailable = package.BillingAddress is not null,
                    BillingAddress = new OrderAddressDto
                    {
                        FullAddress = package.BillingAddress?.Address,
                        City = package.BillingAddress?.City,
                        District = package.BillingAddress?.District,
                        PostalCode = package.BillingAddress?.PostalCode,
                        CountryCode = "TR"
                    }
                },
                Lines = lines
            };
        }).ToList();

        return items.FirstOrDefault(x =>
            x.OrderNumber.Equals(externalOrderId, StringComparison.OrdinalIgnoreCase) ||
            x.OrderId.Equals(externalOrderId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<InvoiceOperationResultDto> CreateInvoiceAsync(InvoiceCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.InvoiceUrl))
            throw new ProviderValidationException("N11 fatura linki için InvoiceUrl zorunludur.");
        if (string.IsNullOrWhiteSpace(request.OrderId))
            throw new ProviderValidationException("N11 için sipariş numarası (OrderId) zorunludur.");

        var soapRequest = new SaveLinkSellerInvoiceRequest
        {
            auth = new Authentication
            {
                appKey = _credentials.ApiKey,
                appSecret = _credentials.ApiSecret
            },
            url = request.InvoiceUrl,
            orderNumber = request.OrderId
        };

        await using var client = new SellerInvoiceServicePortClient();
        var response = await client.SaveLinkSellerInvoiceAsync(soapRequest);
        var body = response.SaveLinkSellerInvoiceResponse;
        var ri = body?.result;
        if (ri is not null && !string.IsNullOrWhiteSpace(ri.errorMessage))
            throw new ExternalProviderException($"N11 fatura servisi: {ri.errorMessage}", null);

        var success = ri?.status is null ||
                      string.Equals(ri.status, "success", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(ri.status, "Success", StringComparison.OrdinalIgnoreCase);

        return new InvoiceOperationResultDto
        {
            Success = success,
            ProviderMessage = success ? "N11 fatura linki kaydedildi." : (ri?.errorMessage ?? "N11 yanıtı işlenemedi."),
            ExternalInvoiceId = request.ExternalInvoiceNumber,
            DocumentUrl = body?.url ?? request.InvoiceUrl
        };
    }
}
