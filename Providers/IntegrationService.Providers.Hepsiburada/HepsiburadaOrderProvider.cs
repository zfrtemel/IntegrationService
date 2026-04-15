using IntegrationService.Core.Contracts;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Models.Invoices;
using IntegrationService.Core.Models.Orders;
using IntegrationService.Core.Providers;
using IntegrationService.Providers.Hepsiburada.Models.CreateInvoice;
using IntegrationService.Providers.Hepsiburada.Models.OpenLineItems;
using IntegrationService.Providers.Hepsiburada.Models.OrderDetail;
using IntegrationService.Providers.Hepsiburada.Models.OrderLists;
using RestSharp;
using RestSharp.Serializers.Json;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IntegrationService.Providers.Hepsiburada;

public sealed class HepsiburadaOrderProvider : IOrderProvider, IInvoiceProvider
{
    private static readonly JsonSerializerOptions HbJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly HepsiburadaCredentials _credentials;
    private readonly RestClient _restClient;

    public HepsiburadaOrderProvider(HepsiburadaCredentials credentials)
    {
        _credentials = credentials;
        _credentials.Validate();
        //if (string.IsNullOrWhiteSpace(_credentials.Username) || string.IsNullOrWhiteSpace(_credentials.Password))
        //    throw new InvalidOperationException($"{HepsiburadaProviderOptions.SectionName}:Credentials zorunludur.");
        var hbAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_credentials.Username}:{_credentials.Password}"));
        _restClient = new RestClient(
            new Uri("https://oms-external-sit.hepsiburada.com/", UriKind.Absolute),
            configureRestClient: opt => opt.UserAgent = null,
            configureDefaultHeaders: headers =>
            {
                headers.TryAddWithoutValidation("accept", "application/json");
                headers.TryAddWithoutValidation("authorization", $"Basic {hbAuth}");
                headers.TryAddWithoutValidation("user-Agent", $"websitefabrikasi_dev");
            },
            configureSerialization: cfg => cfg.UseSystemTextJson(HbJsonOptions));
    }

    public IntegrationProviderType Provider => IntegrationProviderType.Hepsiburada;

    public async Task<UnifiedOrderPageDto> GetOrdersAsync(UnifiedOrderQuery query, CancellationToken cancellationToken = default)
    {
        var normalizedStatus = NormalizeStatusOrThrow(query.Status);
        if (normalizedStatus is not null)
            return await ListSingleChannelAsync(normalizedStatus, query, cancellationToken).ConfigureAwait(false);

        return await ListMergedAllChannelsAsync(query, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UnifiedOrderDto?> GetOrderDetailAsync(string orderIdOrNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderIdOrNumber))
            return null;

        var path =
            $"orders/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}/ordernumber/{Uri.EscapeDataString(orderIdOrNumber.Trim())}";
        var restRequest = new RestRequest(path);
        var restResponse = await _restClient.ExecuteAsync<HbOrderDetailResponse>(restRequest, cancellationToken).ConfigureAwait(false);
        if (restResponse.StatusCode == HttpStatusCode.NotFound)
            return null;
        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Hepsiburada API kimlik doğrulaması başarısız.");
        if (!restResponse.IsSuccessful)
            throw new ExternalProviderException($"Hepsiburada sipariş detayı hatası: {(int)restResponse.StatusCode}", restResponse.StatusCode);

        var data = restResponse.Data;
        if (data is null)
            return null;

        return MapOrderDetail(data);
    }

    public async Task<InvoiceOperationResultDto> CreateInvoiceAsync(InvoiceCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
            throw new ProviderValidationException("Fatura gönderimi için OrderId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.InvoiceUrl))
            throw new ProviderValidationException("Hepsiburada fatura linki için InvoiceUrl zorunludur.");

        var endpoint = $"packages/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}/packagenumber/{Uri.EscapeDataString(request.OrderId)}/invoice";
        var now = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var row = request.ExternalInvoiceNumber ?? "1";
        var serial = request.ExternalInvoiceNumber ?? "HBINV";
        var payload = new CreateInvoiceRequest
        {
            ArrangementDate = now,
            InvoiceLink = request.InvoiceUrl!,
            RowNumber = row,
            SerialNumber = serial,
            Invoices =
            [
                new CreateInvoiceRequestItem
                {
                    ArrangementDate = now,
                    InvoiceLink = request.InvoiceUrl!,
                    OrderNumber = request.OrderId!,
                    RowNumber = row,
                    SerialNumber = serial
                }
            ]
        };

        var restRequest = new RestRequest(endpoint, Method.Put);
        restRequest.AddStringBody(JsonSerializer.Serialize(payload), ContentType.Json);

        var restResponse = await _restClient.ExecuteAsync(restRequest, cancellationToken).ConfigureAwait(false);
        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Hepsiburada API kimlik doğrulaması başarısız.");
        if (!restResponse.IsSuccessful)
            throw new ExternalProviderException($"Hepsiburada invoice endpoint hatası: {(int)restResponse.StatusCode}", restResponse.StatusCode);

        return new InvoiceOperationResultDto
        {
            Success = true,
            ProviderMessage = "Hepsiburada fatura linki başarılı şekilde gönderildi.",
            ExternalInvoiceId = request.ExternalInvoiceNumber,
            DocumentUrl = request.InvoiceUrl
        };
    }

    private static string? NormalizeStatusOrThrow(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        var t = status.Trim();
        foreach (var known in HepsiburadaOrderStatuses.All)
        {
            if (t.Equals(known, StringComparison.OrdinalIgnoreCase))
                return known;
        }

        throw new ProviderValidationException(
            $"Geçersiz Hepsiburada Status: '{status}'. İzin verilen değerler: {string.Join(", ", HepsiburadaOrderStatuses.All)}.");
    }

    private async Task<UnifiedOrderPageDto> ListSingleChannelAsync(string channel, UnifiedOrderQuery query, CancellationToken cancellationToken)
    {
        var pageIndex = Math.Max(0, query.Page);
        var maxLimit = GetMaxLimitForChannel(channel);
        var pageSize = Math.Clamp(query.Size <= 0 ? Math.Min(50, maxLimit) : query.Size, 1, maxLimit);
        var offset = pageIndex * pageSize;

        var page = await FetchChannelPageAsync(channel, offset, pageSize, cancellationToken).ConfigureAwait(false);
        var totalPages = page.PageCount > 0
            ? page.PageCount
            : Math.Max(1, (int)Math.Ceiling(page.TotalCount / (double)pageSize));

        return new UnifiedOrderPageDto
        {
            Items = page.Items,
            Page = pageIndex,
            Size = pageSize,
            TotalElements = page.TotalCount,
            TotalPages = totalPages
        };
    }

    private async Task<UnifiedOrderPageDto> ListMergedAllChannelsAsync(UnifiedOrderQuery query, CancellationToken cancellationToken)
    {
        var pageIndex = Math.Max(0, query.Page);
        var pageSize = Math.Clamp(query.Size <= 0 ? 50 : query.Size, 1, 200);

        var results = await Task.WhenAll(HepsiburadaOrderStatuses.All.Select(c => FetchAllOrdersForChannelAsync(c, cancellationToken)))
            .ConfigureAwait(false);

        var merged = results.SelectMany(x => x).ToList();
        merged = DedupeOrders(merged);
        merged.Sort((a, b) => DateTimeOffset.Compare(
            b.CreatedAt ?? DateTimeOffset.MinValue,
            a.CreatedAt ?? DateTimeOffset.MinValue));

        var totalElements = merged.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalElements / (double)pageSize));
        var slice = merged.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        return new UnifiedOrderPageDto
        {
            Items = slice,
            Page = pageIndex,
            Size = pageSize,
            TotalElements = totalElements,
            TotalPages = totalPages
        };
    }

    private sealed record ChannelPageResult(int TotalCount, int PageCount, IReadOnlyList<UnifiedOrderDto> Items);

    private async Task<ChannelPageResult> FetchChannelPageAsync(string channel, int offset, int limit, CancellationToken cancellationToken)
    {
        return channel switch
        {
            HepsiburadaOrderStatuses.PaymentCompleted => await FetchPaymentCompletedSliceAsync(offset, limit, cancellationToken)
                .ConfigureAwait(false),
            HepsiburadaOrderStatuses.PaymentAwaiting => await FetchPaymentAwaitingSliceAsync(offset, limit, cancellationToken)
                .ConfigureAwait(false),
            HepsiburadaOrderStatuses.Cancelled => await FetchCancelledSliceAsync(offset, limit, cancellationToken).ConfigureAwait(false),
            HepsiburadaOrderStatuses.Shipped => await FetchPackageSliceAsync(
                "shipped", HepsiburadaOrderStatuses.Shipped, offset, limit, cancellationToken).ConfigureAwait(false),
            HepsiburadaOrderStatuses.Delivered => await FetchPackageSliceAsync(
                "delivered", HepsiburadaOrderStatuses.Delivered, offset, limit, cancellationToken).ConfigureAwait(false),
            HepsiburadaOrderStatuses.Undelivered => await FetchPackageSliceAsync(
                "undelivered", HepsiburadaOrderStatuses.Undelivered, offset, limit, cancellationToken).ConfigureAwait(false),
            HepsiburadaOrderStatuses.MissingInvoice => await FetchMissingInvoiceSliceAsync(offset, limit, cancellationToken).ConfigureAwait(false),
            HepsiburadaOrderStatuses.Unpacked => await FetchUnpackedSliceAsync(offset, limit, cancellationToken).ConfigureAwait(false),
            _ => throw new ProviderValidationException($"Desteklenmeyen kanal: {channel}")
        };
    }

    private async Task<List<UnifiedOrderDto>> FetchAllOrdersForChannelAsync(string channel, CancellationToken cancellationToken)
    {
        return channel switch
        {
            HepsiburadaOrderStatuses.PaymentCompleted => await FetchAllPaymentCompletedAsync(cancellationToken).ConfigureAwait(false),
            HepsiburadaOrderStatuses.PaymentAwaiting => await FetchAllPaymentAwaitingAsync(cancellationToken).ConfigureAwait(false),
            HepsiburadaOrderStatuses.Cancelled => await FetchAllCancelledAsync(cancellationToken).ConfigureAwait(false),
            HepsiburadaOrderStatuses.Shipped => await FetchAllPackagesAsync(
                "shipped", HepsiburadaOrderStatuses.Shipped, cancellationToken).ConfigureAwait(false),
            HepsiburadaOrderStatuses.Delivered => await FetchAllPackagesAsync(
                "delivered", HepsiburadaOrderStatuses.Delivered, cancellationToken).ConfigureAwait(false),
            HepsiburadaOrderStatuses.Undelivered => await FetchAllPackagesAsync(
                "undelivered", HepsiburadaOrderStatuses.Undelivered, cancellationToken).ConfigureAwait(false),
            HepsiburadaOrderStatuses.MissingInvoice => await FetchAllMissingInvoiceAsync(cancellationToken).ConfigureAwait(false),
            HepsiburadaOrderStatuses.Unpacked => await FetchAllUnpackedAsync(cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException(channel)
        };
    }

    private static int GetMaxLimitForChannel(string channel) => channel switch
    {
        HepsiburadaOrderStatuses.PaymentCompleted => 100,
        HepsiburadaOrderStatuses.Unpacked => 10,
        _ => 50
    };

    private async Task<ChannelPageResult> FetchPaymentCompletedSliceAsync(int offset, int limit, CancellationToken cancellationToken)
    {
        var path = $"orders/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}?offset={offset}&limit={limit}";
        var restResponse = await _restClient.ExecuteAsync<HbOpenLineItemsPage>(new RestRequest(path), cancellationToken).ConfigureAwait(false);
        ThrowIfListFailed(restResponse);
        var page = restResponse.Data ?? new HbOpenLineItemsPage();
        var items = GroupLinesToOrders(page.Items ?? []);
        return new ChannelPageResult(page.TotalCount, page.PageCount, items);
    }

    private async Task<List<UnifiedOrderDto>> FetchAllPaymentCompletedAsync(CancellationToken cancellationToken)
    {
        const int maxLimit = 100;
        var allLines = new List<HbLineItem>();
        var offset = 0;
        while (true)
        {
            var path = $"orders/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}?offset={offset}&limit={maxLimit}";
            var restResponse = await _restClient.ExecuteAsync<HbOpenLineItemsPage>(new RestRequest(path), cancellationToken).ConfigureAwait(false);
            ThrowIfListFailed(restResponse);
            var page = restResponse.Data ?? new HbOpenLineItemsPage();
            var batch = page.Items ?? [];
            allLines.AddRange(batch);
            if (batch.Count == 0 || batch.Count < maxLimit)
                break;
            offset += maxLimit;
        }

        return GroupLinesToOrders(allLines);
    }

    private async Task<ChannelPageResult> FetchPaymentAwaitingSliceAsync(int offset, int limit, CancellationToken cancellationToken)
    {
        var path =
            $"orders/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}/paymentawaiting?offset={offset}&limit={limit}";
        var restResponse = await _restClient.ExecuteAsync<HbPaymentAwaitingPage>(new RestRequest(path), cancellationToken).ConfigureAwait(false);
        ThrowIfListFailed(restResponse);
        var page = restResponse.Data ?? new HbPaymentAwaitingPage();
        var items = MapPaymentAwaitingToOrders(page.Items ?? []);
        return new ChannelPageResult(page.TotalCount, page.PageCount, items);
    }

    private async Task<List<UnifiedOrderDto>> FetchAllPaymentAwaitingAsync(CancellationToken cancellationToken)
    {
        const int maxLimit = 50;
        var all = new List<HbPaymentAwaitingLine>();
        var offset = 0;
        while (true)
        {
            var path =
                $"orders/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}/paymentawaiting?offset={offset}&limit={maxLimit}";
            var restResponse = await _restClient.ExecuteAsync<HbPaymentAwaitingPage>(new RestRequest(path), cancellationToken).ConfigureAwait(false);
            ThrowIfListFailed(restResponse);
            var page = restResponse.Data ?? new HbPaymentAwaitingPage();
            var batch = page.Items ?? [];
            all.AddRange(batch);
            if (batch.Count == 0 || batch.Count < maxLimit)
                break;
            offset += maxLimit;
        }

        return MapPaymentAwaitingToOrders(all);
    }

    private async Task<ChannelPageResult> FetchCancelledSliceAsync(int offset, int limit, CancellationToken cancellationToken)
    {
        var path = $"orders/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}/cancelled?offset={offset}&limit={limit}";
        var restResponse = await _restClient.ExecuteAsync<HbCancelledPage>(new RestRequest(path), cancellationToken).ConfigureAwait(false);
        ThrowIfListFailed(restResponse);
        var page = restResponse.Data ?? new HbCancelledPage();
        var items = MapCancelledToOrders(page.Items ?? []);
        return new ChannelPageResult(page.TotalCount, page.PageCount, items);
    }

    private async Task<List<UnifiedOrderDto>> FetchAllCancelledAsync(CancellationToken cancellationToken)
    {
        const int maxLimit = 50;
        var all = new List<HbCancelledLine>();
        var offset = 0;
        while (true)
        {
            var path = $"orders/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}/cancelled?offset={offset}&limit={maxLimit}";
            var restResponse = await _restClient.ExecuteAsync<HbCancelledPage>(new RestRequest(path), cancellationToken).ConfigureAwait(false);
            ThrowIfListFailed(restResponse);
            var page = restResponse.Data ?? new HbCancelledPage();
            var batch = page.Items ?? [];
            all.AddRange(batch);
            if (batch.Count == 0 || batch.Count < maxLimit)
                break;
            offset += maxLimit;
        }

        return MapCancelledToOrders(all);
    }

    private async Task<ChannelPageResult> FetchPackageSliceAsync(
        string packagePathSuffix,
        string statusLabel,
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        var path =
            $"packages/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}/{packagePathSuffix}?offset={offset}&limit={limit}";
        var restResponse =
            await _restClient.ExecuteAsync<HbPackagePage<HbPackageRow>>(new RestRequest(path), cancellationToken).ConfigureAwait(false);
        ThrowIfListFailed(restResponse);
        var page = restResponse.Data ?? new HbPackagePage<HbPackageRow>();
        var items = MapPackagesToOrders(page.Items ?? [], statusLabel);
        return new ChannelPageResult(page.TotalCount, page.PageCount, items);
    }

    private async Task<List<UnifiedOrderDto>> FetchAllPackagesAsync(string packagePathSuffix, string statusLabel, CancellationToken cancellationToken)
    {
        const int maxLimit = 50;
        var all = new List<HbPackageRow>();
        var offset = 0;
        while (true)
        {
            var path =
                $"packages/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}/{packagePathSuffix}?offset={offset}&limit={maxLimit}";
            var restResponse =
                await _restClient.ExecuteAsync<HbPackagePage<HbPackageRow>>(new RestRequest(path), cancellationToken).ConfigureAwait(false);
            ThrowIfListFailed(restResponse);
            var page = restResponse.Data ?? new HbPackagePage<HbPackageRow>();
            var batch = page.Items ?? [];
            all.AddRange(batch);
            if (batch.Count == 0 || batch.Count < maxLimit)
                break;
            offset += maxLimit;
        }

        return MapPackagesToOrders(all, statusLabel);
    }

    private async Task<ChannelPageResult> FetchMissingInvoiceSliceAsync(int offset, int limit, CancellationToken cancellationToken)
    {
        var path =
            $"packages/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}/missing-invoice?offset={offset}&limit={limit}";
        var restResponse =
            await _restClient.ExecuteAsync<HbPackagePage<HbMissingInvoicePackage>>(new RestRequest(path), cancellationToken).ConfigureAwait(false);
        ThrowIfListFailed(restResponse);
        var page = restResponse.Data ?? new HbPackagePage<HbMissingInvoicePackage>();
        var items = MapMissingInvoiceToOrders(page.Items ?? []);
        return new ChannelPageResult(page.TotalCount, page.PageCount, items);
    }

    private async Task<List<UnifiedOrderDto>> FetchAllMissingInvoiceAsync(CancellationToken cancellationToken)
    {
        const int maxLimit = 50;
        var all = new List<HbMissingInvoicePackage>();
        var offset = 0;
        while (true)
        {
            var path =
                $"packages/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}/missing-invoice?offset={offset}&limit={maxLimit}";
            var restResponse =
                await _restClient.ExecuteAsync<HbPackagePage<HbMissingInvoicePackage>>(new RestRequest(path), cancellationToken)
                    .ConfigureAwait(false);
            ThrowIfListFailed(restResponse);
            var page = restResponse.Data ?? new HbPackagePage<HbMissingInvoicePackage>();
            var batch = page.Items ?? [];
            all.AddRange(batch);
            if (batch.Count == 0 || batch.Count < maxLimit)
                break;
            offset += maxLimit;
        }

        return MapMissingInvoiceToOrders(all);
    }

    private async Task<ChannelPageResult> FetchUnpackedSliceAsync(int offset, int limit, CancellationToken cancellationToken)
    {
        var path =
            $"packages/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}/status/unpacked?offset={offset}&limit={limit}";
        var restResponse = await _restClient.ExecuteAsync(new RestRequest(path), cancellationToken).ConfigureAwait(false);
        ThrowIfListFailed(restResponse);
        var page = DeserializeUnpackedPage(restResponse.Content) ?? new HbPackagePage<HbUnpackedDelivery>();
        var items = MapUnpackedToOrders(page.Items ?? []);
        return new ChannelPageResult(page.TotalCount, page.PageCount, items);
    }

    private async Task<List<UnifiedOrderDto>> FetchAllUnpackedAsync(CancellationToken cancellationToken)
    {
        const int maxLimit = 10;
        var all = new List<HbUnpackedDelivery>();
        var offset = 0;
        while (true)
        {
            var path =
                $"packages/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}/status/unpacked?offset={offset}&limit={maxLimit}";
            var restResponse = await _restClient.ExecuteAsync(new RestRequest(path), cancellationToken).ConfigureAwait(false);
            ThrowIfListFailed(restResponse);
            var page = DeserializeUnpackedPage(restResponse.Content) ?? new HbPackagePage<HbUnpackedDelivery>();
            var batch = page.Items ?? [];
            all.AddRange(batch);
            if (batch.Count == 0 || batch.Count < maxLimit)
                break;
            offset += maxLimit;
        }

        return MapUnpackedToOrders(all);
    }

    private static HbPackagePage<HbUnpackedDelivery>? DeserializeUnpackedPage(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var el = doc.RootElement;
            if (el.ValueKind == JsonValueKind.Array && el.GetArrayLength() > 0)
                el = el[0];
            return JsonSerializer.Deserialize<HbPackagePage<HbUnpackedDelivery>>(el.GetRawText(), HbJsonOptions);
        }
        catch
        {
            return JsonSerializer.Deserialize<HbPackagePage<HbUnpackedDelivery>>(json, HbJsonOptions);
        }
    }

    private static List<UnifiedOrderDto> MapPaymentAwaitingToOrders(List<HbPaymentAwaitingLine> lines)
    {
        var synthetic = lines.Select(l => new HbLineItem
        {
            Id = l.Id,
            OrderId = l.OrderNumber,
            OrderNumber = l.OrderNumber,
            Name = l.Name,
            Sku = l.Sku,
            MerchantSku = l.MerchantSku,
            Quantity = l.Quantity,
            OrderDate = l.OrderDate,
            Status = HepsiburadaOrderStatuses.PaymentAwaiting
        }).ToList();

        return GroupLinesToOrders(synthetic);
    }

    private static List<UnifiedOrderDto> MapCancelledToOrders(List<HbCancelledLine> lines)
    {
        var groups = lines
            .GroupBy(l => l.OrderNumber ?? l.LineItemId ?? Guid.NewGuid().ToString("N"))
            .ToList();

        var result = new List<UnifiedOrderDto>();
        foreach (var g in groups)
        {
            var linesDto = g.Select((line, i) => new UnifiedOrderLineDto
            {
                LineId = line.LineItemId ?? i.ToString(CultureInfo.InvariantCulture),
                Name = string.Empty,
                Sku = line.MerchantSku ?? line.Sku ?? string.Empty,
                Qty = line.Quantity.GetValueOrDefault(1) <= 0 ? 1 : line.Quantity!.Value,
                UnitPrice = 0,
                LineTotal = 0,
                TaxRate = null
            }).ToList();

            var first = g.First();
            var created = ParseHbDate(first.CancelDate) ?? DateTimeOffset.UtcNow;

            result.Add(new UnifiedOrderDto
            {
                OrderId = first.LineItemId ?? first.OrderNumber ?? string.Empty,
                OrderNumber = first.OrderNumber ?? string.Empty,
                Marketplace = IntegrationProviderType.Hepsiburada,
                Status = $"{HepsiburadaOrderStatuses.Cancelled}" + (first.CancelReasonCode is not null ? $" ({first.CancelReasonCode})" : ""),
                CreatedAt = created,
                Customer = new OrderCustomerDto(),
                Amounts = new OrderAmountSummaryDto { Currency = "TRY" },
                ShippingAddress = new OrderAddressDto(),
                BillingInfo = new OrderBillingInfoDto(),
                Lines = linesDto
            });
        }

        return result;
    }

    private static List<UnifiedOrderDto> MapPackagesToOrders(List<HbPackageRow> rows, string statusLabel)
    {
        var list = new List<UnifiedOrderDto>();
        foreach (var p in rows)
        {
            var orderNumber = p.OrderNumber ?? p.OrderNumbers?.FirstOrDefault() ?? string.Empty;
            var when = ParseHbDate(p.DeliveredDate) ?? ParseHbDate(p.ShippedDate) ?? ParseHbDate(p.UndeliveredDate) ?? DateTimeOffset.UtcNow;
            list.Add(new UnifiedOrderDto
            {
                OrderId = p.PackageNumber ?? p.Id ?? string.Empty,
                OrderNumber = orderNumber,
                Marketplace = IntegrationProviderType.Hepsiburada,
                Status = statusLabel,
                CreatedAt = when,
                Customer = new OrderCustomerDto(),
                Amounts = new OrderAmountSummaryDto { Currency = "TRY" },
                ShippingAddress = new OrderAddressDto(),
                BillingInfo = new OrderBillingInfoDto(),
                Lines = Array.Empty<UnifiedOrderLineDto>()
            });
        }

        return list;
    }

    private static List<UnifiedOrderDto> MapMissingInvoiceToOrders(List<HbMissingInvoicePackage> rows)
    {
        var list = new List<UnifiedOrderDto>();
        foreach (var p in rows)
        {
            var orderNumber = p.OrderNumbers?.FirstOrDefault() ?? string.Empty;
            list.Add(new UnifiedOrderDto
            {
                OrderId = p.PackageNumber ?? string.Empty,
                OrderNumber = orderNumber,
                Marketplace = IntegrationProviderType.Hepsiburada,
                Status = $"{HepsiburadaOrderStatuses.MissingInvoice}{(p.Status is not null ? $": {p.Status}" : "")}",
                CreatedAt = DateTimeOffset.UtcNow,
                Customer = new OrderCustomerDto(),
                Amounts = new OrderAmountSummaryDto { Currency = "TRY" },
                ShippingAddress = new OrderAddressDto(),
                BillingInfo = new OrderBillingInfoDto(),
                Lines = Array.Empty<UnifiedOrderLineDto>()
            });
        }

        return list;
    }

    private static List<UnifiedOrderDto> MapUnpackedToOrders(List<HbUnpackedDelivery> rows)
    {
        var list = new List<UnifiedOrderDto>();
        foreach (var p in rows)
        {
            var when = ParseHbDate(p.UnpackedDate) ?? DateTimeOffset.UtcNow;
            list.Add(new UnifiedOrderDto
            {
                OrderId = p.PackageNumber ?? string.Empty,
                OrderNumber = string.Empty,
                Marketplace = IntegrationProviderType.Hepsiburada,
                Status = HepsiburadaOrderStatuses.Unpacked,
                CreatedAt = when,
                Customer = new OrderCustomerDto(),
                Amounts = new OrderAmountSummaryDto { Currency = "TRY" },
                ShippingAddress = new OrderAddressDto(),
                BillingInfo = new OrderBillingInfoDto(),
                Lines = Array.Empty<UnifiedOrderLineDto>()
            });
        }

        return list;
    }

    private static List<UnifiedOrderDto> DedupeOrders(List<UnifiedOrderDto> items)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<UnifiedOrderDto>();
        foreach (var o in items)
        {
            var key = $"{o.OrderNumber}\u001f{o.OrderId}";
            if (seen.Add(key))
                result.Add(o);
        }

        return result;
    }

    private static UnifiedOrderDto MapOrderDetail(HbOrderDetailResponse d)
    {
        var lines = new List<UnifiedOrderLineDto>();
        var idx = 0;
        foreach (var line in d.Items ?? [])
        {
            var qty = line.Quantity ?? 0;
            var unit = line.UnitPrice?.Amount ?? 0m;
            var total = line.TotalPrice?.Amount ?? unit * Math.Max(qty, 1);
            int? tax = line.VatRate is null ? null : (int)Math.Round(line.VatRate.Value, MidpointRounding.AwayFromZero);
            lines.Add(new UnifiedOrderLineDto
            {
                LineId = line.Id ?? idx.ToString(CultureInfo.InvariantCulture),
                Name = line.Name ?? string.Empty,
                Sku = line.MerchantSKU ?? line.Sku ?? string.Empty,
                Qty = qty <= 0 ? 1 : qty,
                UnitPrice = unit,
                LineTotal = total,
                TaxRate = tax
            });
            idx++;
        }

        var firstLine = d.Items?.FirstOrDefault();
        var ship = firstLine?.ShippingAddress ?? d.DeliveryAddress;
        var inv = firstLine?.Invoice ?? d.Invoice;

        var billing = new OrderBillingInfoDto
        {
            InvoiceType = !string.IsNullOrWhiteSpace(inv?.TaxNumber) ? "Corporate" : "Individual",
            BillingFullNameOrCompany = inv?.Address?.Name ?? d.Customer?.Name,
            TaxOffice = inv?.TaxOffice,
            TaxNumber = inv?.TaxNumber,
            Tckn = inv?.TurkishIdentityNumber,
            InvoiceAvailable = inv is not null,
            BillingAddress = MapAddress(inv?.Address)
        };

        var created = ParseHbDate(d.OrderDate) ?? ParseHbDate(d.CreatedDate) ?? DateTimeOffset.UtcNow;

        return new UnifiedOrderDto
        {
            OrderId = d.OrderId ?? string.Empty,
            OrderNumber = d.OrderNumber ?? string.Empty,
            Marketplace = IntegrationProviderType.Hepsiburada,
            Status = firstLine?.Status ?? d.PaymentStatus,
            CreatedAt = created,
            Customer = new OrderCustomerDto
            {
                FullName = d.Customer?.Name ?? firstLine?.CustomerName,
                Email = ship?.Email
            },
            Amounts = new OrderAmountSummaryDto
            {
                Subtotal = lines.Sum(x => x.UnitPrice * x.Qty),
                GrandTotal = lines.Sum(x => x.LineTotal),
                Currency = firstLine?.TotalPrice?.Currency ?? firstLine?.UnitPrice?.Currency ?? "TRY"
            },
            ShippingAddress = MapAddress(ship) ?? new OrderAddressDto(),
            BillingInfo = billing,
            Lines = lines
        };
    }

    private void ThrowIfListFailed(RestResponse restResponse)
    {
        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Hepsiburada API kimlik doğrulaması başarısız.");
        if (!restResponse.IsSuccessful)
            throw new ExternalProviderException($"Hepsiburada sipariş listesi hatası: {(int)restResponse.StatusCode}", restResponse.StatusCode);
    }

    private static List<UnifiedOrderDto> GroupLinesToOrders(List<HbLineItem> lines)
    {
        var groups = lines
            .GroupBy(l => !string.IsNullOrWhiteSpace(l.PackageNumber) ? l.PackageNumber! : l.OrderId ?? l.Id ?? Guid.NewGuid().ToString("N"))
            .ToList();

        var result = new List<UnifiedOrderDto>();
        foreach (var g in groups)
        {
            var first = g.First();
            var orderLines = g.Select((line, i) => MapLine(line, i)).ToList();
            var grand = orderLines.Sum(x => x.LineTotal);
            var sub = orderLines.Sum(x => x.UnitPrice * x.Qty);

            var billing = MapBilling(first);
            var created = ParseHbDate(first.OrderDate) ?? DateTimeOffset.UtcNow;

            result.Add(new UnifiedOrderDto
            {
                OrderId = first.PackageNumber ?? first.Id ?? string.Empty,
                OrderNumber = first.OrderNumber ?? string.Empty,
                Marketplace = IntegrationProviderType.Hepsiburada,
                Status = first.Status ?? string.Empty,
                CreatedAt = created,
                Customer = new OrderCustomerDto
                {
                    FullName = first.CustomerName,
                    Email = first.ShippingAddress?.Email
                },
                Amounts = new OrderAmountSummaryDto
                {
                    Subtotal = sub,
                    GrandTotal = grand,
                    Currency = first.TotalPrice?.Currency ?? first.UnitPrice?.Currency ?? "TRY"
                },
                ShippingAddress = MapAddress(first.ShippingAddress) ?? new OrderAddressDto(),
                BillingInfo = billing,
                Lines = orderLines
            });
        }

        return result;
    }

    private static OrderBillingInfoDto MapBilling(HbLineItem line)
    {
        var inv = line.Invoice;
        var corp = !string.IsNullOrWhiteSpace(inv?.TaxNumber);
        return new OrderBillingInfoDto
        {
            InvoiceType = corp ? "Corporate" : "Individual",
            BillingFullNameOrCompany = inv?.Address?.Name ?? line.CustomerName,
            TaxOffice = inv?.TaxOffice,
            TaxNumber = inv?.TaxNumber,
            Tckn = inv?.TurkishIdentityNumber,
            InvoiceAvailable = inv is not null,
            BillingAddress = MapAddress(inv?.Address)
        };
    }

    private static OrderAddressDto? MapAddress(HbAddress? a)
    {
        if (a is null)
            return null;
        return new OrderAddressDto
        {
            FullAddress = a.Address,
            City = a.City,
            District = a.Town ?? a.District,
            PostalCode = a.PostalCode,
            CountryCode = string.IsNullOrWhiteSpace(a.CountryCode) ? "TR" : a.CountryCode
        };
    }

    private static OrderAddressDto? MapAddress(HbDetailAddress? a)
    {
        if (a is null)
            return null;
        return new OrderAddressDto
        {
            FullAddress = a.Address,
            City = a.City,
            District = a.Town ?? a.District,
            PostalCode = a.PostalCode,
            CountryCode = string.IsNullOrWhiteSpace(a.CountryCode) ? "TR" : a.CountryCode
        };
    }

    private static UnifiedOrderLineDto MapLine(HbLineItem line, int index)
    {
        var qty = line.Quantity ?? 0;
        var unit = line.UnitPrice?.Amount ?? 0m;
        var total = line.TotalPrice?.Amount ?? unit * Math.Max(qty, 1);
        int? tax = line.VatRate is null ? null : (int)Math.Round(line.VatRate.Value, MidpointRounding.AwayFromZero);
        return new UnifiedOrderLineDto
        {
            LineId = line.Id ?? index.ToString(CultureInfo.InvariantCulture),
            Name = line.Name ?? string.Empty,
            Sku = line.MerchantSku ?? line.Sku ?? string.Empty,
            Qty = qty <= 0 ? 1 : qty,
            UnitPrice = unit,
            LineTotal = total,
            TaxRate = tax
        };
    }

    private static DateTimeOffset? ParseHbDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;
        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
            return dto;
        return null;
    }
}
