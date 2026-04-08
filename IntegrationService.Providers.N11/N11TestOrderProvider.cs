using IntegrationService.Core.Contracts;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Models.Invoices;
using IntegrationService.Core.Models.Orders;
using IntegrationService.Core.Models.Providers;
using IntegrationService.Core.Providers;
using System.Net;
using System.Text.Json;

namespace IntegrationService.Providers.N11;

public sealed class N11TestOrderProvider : IOrderProvider, IInvoiceProvider
{
    private readonly N11Credentials _credentials;
    private readonly HttpClient _httpClient;

    public N11TestOrderProvider(N11Credentials credentials, HttpClient httpClient)
    {
        _credentials = credentials;
        _httpClient = httpClient;
        _credentials.Validate();
    }

    public IntegrationProviderType Provider => IntegrationProviderType.N11;
    public ProviderCapabilities Capabilities => new()
    {
        SupportsOrderBillingFields = true,
        SupportsInvoiceCreate = false,
        SupportsInvoiceCancel = false,
        SupportsInvoiceDownload = false
    };

    public async Task<UnifiedOrderPageDto> GetOrdersAsync(UnifiedOrderQuery query, CancellationToken cancellationToken = default)
    {
        var payload = await FetchShipmentPackagesAsync(query, null, cancellationToken);

        var items = (payload?.Content ?? []).Select(MapOrder).ToList();

        var page = Math.Max(0, query.Page);
        var size = Math.Clamp(query.Size <= 0 ? 50 : query.Size, 1, 100);
        var slice = items.Skip(page * size).Take(size).ToList();

        return new UnifiedOrderPageDto
        {
            Items = slice,
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
        var packageIds = long.TryParse(externalOrderId, out var pid) ? externalOrderId : null;
        var payload = await FetchShipmentPackagesAsync(query, packageIds, cancellationToken);
        var items = (payload?.Content ?? []).Select(MapOrder).ToList();
        return items.FirstOrDefault(x =>
            x.OrderNumber.Equals(externalOrderId, StringComparison.OrdinalIgnoreCase) ||
            x.OrderId.Equals(externalOrderId, StringComparison.OrdinalIgnoreCase));
    }

    public Task<InvoiceOperationResultDto> CreateInvoiceAsync(InvoiceCreateRequestDto request, CancellationToken cancellationToken = default)
        => throw new NotSupportedByProviderException("N11 public dokümanında net fatura create endpointi olmadığı için placeholder kullanılıyor.");

    public Task<InvoiceOperationResultDto> CancelInvoiceAsync(InvoiceCancelRequestDto request, CancellationToken cancellationToken = default)
        => throw new NotSupportedByProviderException("N11 fatura iptal operasyonu bu adaptörde desteklenmiyor.");

    public Task<InvoiceDocumentResultDto> GetInvoiceDocumentAsync(InvoiceDocumentRequestDto request, CancellationToken cancellationToken = default)
        => throw new NotSupportedByProviderException("N11 fatura dökümanı operasyonu bu adaptörde desteklenmiyor.");

    private static UnifiedOrderDto MapOrder(N11ShipmentPackage package)
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
            CreatedAt = ParseUnixMs(package.LastModifiedDate) ?? ParseUnixMs(package.AgreedDeliveryDate),
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
    }

    private static string BuildShipmentPackagesPath(UnifiedOrderQuery query)
    {
        var page = Math.Max(0, query.Page);
        var size = Math.Clamp(query.Size <= 0 ? 50 : query.Size, 1, 100);
        var start = query.StartDateUnixMs ?? DateTimeOffset.UtcNow.AddDays(-2).ToUnixTimeMilliseconds();
        var end = query.EndDateUnixMs ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var parts = new List<string>
        {
            $"startDate={start}",
            $"endDate={end}",
            $"page={page}",
            $"size={size}",
            "orderByDirection=DESC"
        };

        if (!string.IsNullOrWhiteSpace(query.OrderNumber))
            parts.Add($"orderNumber={Uri.EscapeDataString(query.OrderNumber)}");
        if (!string.IsNullOrWhiteSpace(query.Status))
            parts.Add($"status={Uri.EscapeDataString(query.Status)}");

        return "rest/delivery/v1/shipmentPackages?" + string.Join("&", parts);
    }

    private async Task<N11ShipmentPackagesResponse?> FetchShipmentPackagesAsync(
        UnifiedOrderQuery query,
        string? packageIds,
        CancellationToken cancellationToken)
    {
        var path = BuildShipmentPackagesPath(query, packageIds);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("appkey", _credentials.ApiKey);
        request.Headers.TryAddWithoutValidation("appsecret", _credentials.ApiSecret);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("N11 API kimlik doğrulaması başarısız.");
        if (!response.IsSuccessStatusCode)
            throw new ExternalProviderException($"N11 shipmentPackages hatası: {(int)response.StatusCode}", response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<N11ShipmentPackagesResponse>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);
    }

    private static string BuildShipmentPackagesPath(UnifiedOrderQuery query, string? packageIds)
    {
        var basePath = BuildShipmentPackagesPath(query);
        if (string.IsNullOrWhiteSpace(packageIds))
            return basePath;

        return basePath + $"&packageIds={Uri.EscapeDataString(packageIds)}";
    }

    private static DateTimeOffset? ParseUnixMs(long? value)
        => value.HasValue && value.Value > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(value.Value) : null;
}
