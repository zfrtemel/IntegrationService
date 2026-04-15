using IntegrationService.Core.Contracts;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Models.Invoices;
using IntegrationService.Core.Models.Orders;
using IntegrationService.Core.Providers;
using IntegrationService.Providers.Pttavm.Models.CreateInvoice;
using IntegrationService.Providers.Pttavm.Models.SearchOrders;
using RestSharp;
using RestSharp.Serializers.Json;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace IntegrationService.Providers.Pttavm;

public sealed class PttavmOrderProvider : IOrderProvider, IInvoiceProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly PttavmCredentials _credentials;
    private readonly RestClient _restClient;

    public PttavmOrderProvider(PttavmCredentials credentials)
    {
        _credentials = credentials;
        _credentials.Validate();
        _restClient = new RestClient(
            new Uri("https://integration-api.pttavm.com/", UriKind.Absolute),
            configureDefaultHeaders: headers => headers.TryAddWithoutValidation("Accept", "application/json"),
            configureSerialization: cfg => cfg.UseSystemTextJson(JsonOptions));
    }

    public IntegrationProviderType Provider => IntegrationProviderType.Pttavm;

    public async Task<UnifiedOrderPageDto> GetOrdersAsync(UnifiedOrderQuery query, CancellationToken cancellationToken = default)
    {
        var end = query.EndDateUnixMs.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(query.EndDateUnixMs.Value)
            : DateTimeOffset.UtcNow;
        var start = query.StartDateUnixMs.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(query.StartDateUnixMs.Value)
            : end.AddDays(-7);
        if (end < start)
            (start, end) = (end, start);
        if ((end - start).TotalDays > 40)
            end = start.AddDays(40);
        var startIso = start.ToString("o", CultureInfo.InvariantCulture);
        var endIso = end.ToString("o", CultureInfo.InvariantCulture);

        var path = $"api/v1/orders/search?startDate={Uri.EscapeDataString(startIso)}&endDate={Uri.EscapeDataString(endIso)}&isActiveOrders=false";
        var restRequest = new RestRequest(path);
        restRequest.AddHeader("Api-Key", _credentials.ApiKey);
        restRequest.AddHeader("access-token", _credentials.AccessToken);
        restRequest.AddHeader("X-Correlation-Id", _credentials.CorrelationId);

        var restResponse = await _restClient.ExecuteAsync<List<PttOrderRow>>(restRequest, cancellationToken);
        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("PttAVM API kimlik doğrulaması başarısız.");
        if (!restResponse.IsSuccessful)
        {
            var text = restResponse.Content ?? string.Empty;
            throw new ExternalProviderException(
                string.IsNullOrWhiteSpace(text) ? $"PttAVM API hatası: {(int)restResponse.StatusCode}" : text,
                restResponse.StatusCode);
        }

        var rows = restResponse.Data ?? [];
        var mapped = rows.Select(MapOrder).ToList();

        var page = Math.Max(0, query.Page);
        var size = Math.Clamp(query.Size <= 0 ? 50 : query.Size, 1, 200);
        var slice = mapped.Skip(page * size).Take(size).ToList();

        return new UnifiedOrderPageDto
        {
            Items = slice,
            Page = page,
            Size = size,
            TotalElements = mapped.Count,
            TotalPages = Math.Max(1, (int)Math.Ceiling(mapped.Count / (double)size))
        };
    }

    public async Task<UnifiedOrderDto?> GetOrderDetailAsync(string orderIdOrNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderIdOrNumber))
            return null;

        var path = $"api/v1/orders/{Uri.EscapeDataString(orderIdOrNumber)}";
        var restRequest = new RestRequest(path);
        restRequest.AddHeader("Api-Key", _credentials.ApiKey);
        restRequest.AddHeader("access-token", _credentials.AccessToken);
        restRequest.AddHeader("X-Correlation-Id", _credentials.CorrelationId);

        var restResponse = await _restClient.ExecuteAsync<List<PttOrderRow>>(restRequest, cancellationToken);
        if (restResponse.StatusCode == HttpStatusCode.NotFound)
            return null;
        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("PttAVM API kimlik doğrulaması başarısız.");
        if (!restResponse.IsSuccessful)
        {
            var text = restResponse.Content ?? string.Empty;
            throw new ExternalProviderException(
                string.IsNullOrWhiteSpace(text) ? $"PttAVM API hatası: {(int)restResponse.StatusCode}" : text,
                restResponse.StatusCode);
        }

        var first = restResponse.Data?.FirstOrDefault();
        return first is null ? null : MapOrder(first);
    }

    public async Task<InvoiceOperationResultDto> CreateInvoiceAsync(InvoiceCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.LineItemIds.Count == 0)
            throw new ProviderValidationException("PttAVM fatura gönderimi için lineItemIds alanı zorunludur.");
        if (string.IsNullOrWhiteSpace(request.OrderId))
            throw new ProviderValidationException("PttAVM fatura için OrderId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.PdfBase64) && string.IsNullOrWhiteSpace(request.InvoiceUrl))
            throw new ProviderValidationException("PttAVM fatura gönderimi için PdfBase64 veya InvoiceUrl alanlarından biri zorunludur.");

        var path = $"api/v1/orders/{Uri.EscapeDataString(request.OrderId)}/invoice";
        var body = new CreateInvoiceRequest
        {
            LineItemId = request.LineItemIds.ToList(),
            Content = string.IsNullOrWhiteSpace(request.PdfBase64) ? null : request.PdfBase64,
            Url = string.IsNullOrWhiteSpace(request.InvoiceUrl) ? null : request.InvoiceUrl
        };

        var restRequest = new RestRequest(path, Method.Post);
        restRequest.AddHeader("Api-Key", _credentials.ApiKey);
        restRequest.AddHeader("access-token", _credentials.AccessToken);
        restRequest.AddHeader("X-Correlation-Id", _credentials.CorrelationId);
        restRequest.AddStringBody(JsonSerializer.Serialize(body), ContentType.Json);

        var restResponse = await _restClient.ExecuteAsync<CreateInvoiceResponse>(restRequest, cancellationToken);
        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("PttAVM API kimlik doğrulaması başarısız.");
        if (!restResponse.IsSuccessful)
        {
            var text = restResponse.Content ?? string.Empty;
            throw new ExternalProviderException(
                string.IsNullOrWhiteSpace(text) ? $"PttAVM API hatası: {(int)restResponse.StatusCode}" : text,
                restResponse.StatusCode);
        }

        var result = restResponse.Data;

        return new InvoiceOperationResultDto
        {
            Success = result?.Success ?? true,
            ProviderMessage = result?.ErrorMessage ?? "PttAVM fatura isteği tamamlandı.",
            ExternalInvoiceId = request.ExternalInvoiceNumber ?? $"ptt-inv-{request.OrderId}"
        };
    }

    private static UnifiedOrderDto MapOrder(PttOrderRow row)
    {
        var lines = (row.SiparisUrunler ?? []).Select((u, i) => new UnifiedOrderLineDto
        {
            LineId = u.LineItemId?.ToString(CultureInfo.InvariantCulture) ?? i.ToString(CultureInfo.InvariantCulture),
            Sku = u.UrunKodu ?? string.Empty,
            Name = u.UrunAdi ?? string.Empty,
            Qty = u.ToplamIslemAdedi ?? 1,
            UnitPrice = (decimal)(u.KdvHaricTutar ?? 0),
            LineTotal = (decimal)(u.KdvDahilToplamTutar ?? u.KdvHaricToplamTutar ?? 0),
            TaxRate = u.KdvOrani is null ? null : (int)Math.Round(u.KdvOrani.Value, MidpointRounding.AwayFromZero)
        }).ToList();

        var grand = lines.Sum(l => l.LineTotal);
        var customerName = string.Join(" ", new[] { row.MusteriAdi, row.MusteriSoyadi }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new UnifiedOrderDto
        {
            OrderId = row.SiparisNo ?? string.Empty,
            OrderNumber = row.SiparisNo ?? string.Empty,
            Marketplace = IntegrationProviderType.Pttavm,
            Status = row.SiparisUrunler?.FirstOrDefault()?.SiparisDurumu ?? string.Empty,
            CreatedAt = row.IslemTarihi ?? DateTimeOffset.UtcNow,
            Customer = new OrderCustomerDto
            {
                FullName = string.IsNullOrWhiteSpace(customerName) ? row.MusteriAdi : customerName,
                Email = row.Eposta,
                PhoneMasked = row.TelefonNo
            },
            Amounts = new OrderAmountSummaryDto
            {
                Subtotal = grand,
                Shipping = (decimal)(row.KargoTutari ?? 0),
                GrandTotal = grand + (decimal)(row.KargoTutari ?? 0),
                Currency = "TRY"
            },
            ShippingAddress = new OrderAddressDto
            {
                FullAddress = row.SiparisAdresi,
                City = row.SiparisIli,
                District = row.SiparisIlce
            },
            BillingInfo = new OrderBillingInfoDto
            {
                InvoiceType = string.IsNullOrWhiteSpace(row.FaturaTip) ? "Unknown" : row.FaturaTip,
                BillingFullNameOrCompany = string.Join(" ", new[] { row.FaturaMusteriAdi, row.FaturaMusteriSoyadi }.Where(s => !string.IsNullOrWhiteSpace(s))),
                TaxOffice = row.VergiDaire,
                TaxNumber = row.VergiNo,
                Tckn = row.Tckn,
                InvoiceAvailable = true,
                BillingAddress = new OrderAddressDto
                {
                    FullAddress = row.FaturaAdresi,
                    City = row.FaturaIli,
                    District = row.FaturaIlce
                }
            },
            Lines = lines
        };
    }
}
