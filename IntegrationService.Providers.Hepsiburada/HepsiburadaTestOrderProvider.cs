using IntegrationService.Core.Contracts;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Models.Invoices;
using IntegrationService.Core.Models.Orders;
using IntegrationService.Core.Models.Providers;
using IntegrationService.Core.Providers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IntegrationService.Providers.Hepsiburada;

public sealed class HepsiburadaTestOrderProvider : IOrderProvider, IInvoiceProvider
{
    private readonly HepsiburadaCredentials _credentials;
    private readonly HttpClient _httpClient;

    public HepsiburadaTestOrderProvider(HepsiburadaCredentials credentials, HttpClient httpClient)
    {
        _credentials = credentials;
        _httpClient = httpClient;
        _credentials.Validate();
    }

    public IntegrationProviderType Provider => IntegrationProviderType.Hepsiburada;
    public ProviderCapabilities Capabilities => new()
    {
        SupportsOrderBillingFields = true,
        SupportsInvoiceCreate = true,
        SupportsInvoiceCancel = false,
        SupportsInvoiceDownload = false
    };

    public Task<UnifiedOrderPageDto> GetOrdersAsync(UnifiedOrderQuery query, CancellationToken cancellationToken = default)
    {
        var merchant = _credentials.MerchantId;
        var items = new List<UnifiedOrderDto>
        {
            new()
            {
                OrderId = $"hb-pkg-{merchant}-1",
                OrderNumber = $"HB-ORD-{merchant}-1001",
                Marketplace = Provider,
                Status = "TestCreated",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                Customer = new OrderCustomerDto { FullName = $"HB Test Müşteri ({merchant})", Email = "buyer@hb.example" },
                Amounts = new OrderAmountSummaryDto { GrandTotal = 199.90m, Subtotal = 199.90m, Currency = "TRY" },
                ShippingAddress = new OrderAddressDto { FullAddress = "HB Test Teslimat Adresi", City = "İstanbul", District = "Şişli", CountryCode = "TR" },
                BillingInfo = new OrderBillingInfoDto
                {
                    InvoiceType = "Individual",
                    BillingFullNameOrCompany = "HB Bireysel Müşteri",
                    TaxNumber = null,
                    InvoiceAvailable = false,
                    BillingAddress = new OrderAddressDto { FullAddress = "HB Test Fatura Adresi", City = "İstanbul", District = "Şişli", CountryCode = "TR" }
                },
                Lines =
                [
                    new UnifiedOrderLineDto { LineId = "1", Name = "HB Test Ürün 1", Sku = "HB-SKU-1", Qty = 1, UnitPrice = 199.90m, LineTotal = 199.90m, TaxRate = 20 }
                ]
            },
            new()
            {
                OrderId = $"hb-pkg-{merchant}-2",
                OrderNumber = $"HB-ORD-{merchant}-1002",
                Marketplace = Provider,
                Status = "TestShipped",
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-5),
                Customer = new OrderCustomerDto { FullName = "HB Test Kargo", Email = "logistics@hb.example" },
                Amounts = new OrderAmountSummaryDto { GrandTotal = 459.00m, Subtotal = 430m, Shipping = 29m, Currency = "TRY" },
                ShippingAddress = new OrderAddressDto { FullAddress = "HB Kargo Adresi", City = "Ankara", District = "Çankaya", CountryCode = "TR" },
                BillingInfo = new OrderBillingInfoDto
                {
                    InvoiceType = "Corporate",
                    BillingFullNameOrCompany = "HB Kurumsal A.Ş.",
                    TaxOffice = "Çankaya",
                    TaxNumber = "1234567890",
                    InvoiceAvailable = true,
                    BillingAddress = new OrderAddressDto { FullAddress = "Kurumsal Fatura Adresi", City = "Ankara", District = "Çankaya", CountryCode = "TR" }
                },
                Lines =
                [
                    new UnifiedOrderLineDto { LineId = "2", Name = "HB Test Ürün 2", Sku = "HB-SKU-2", Qty = 1, UnitPrice = 459m, LineTotal = 459m, TaxRate = 20 }
                ]
            }
        };

        var page = Math.Max(0, query.Page);
        var size = Math.Clamp(query.Size <= 0 ? 50 : query.Size, 1, 200);
        var slice = items.Skip(page * size).Take(size).ToList();

        return Task.FromResult(new UnifiedOrderPageDto
        {
            Items = slice,
            Page = page,
            Size = size,
            TotalPages = 1,
            TotalElements = items.Count
        });
    }

    public Task<UnifiedOrderDto?> GetOrderDetailAsync(string externalOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalOrderId))
            return Task.FromResult<UnifiedOrderDto?>(null);

        return GetOrdersAsync(new UnifiedOrderQuery(), cancellationToken)
            .ContinueWith<UnifiedOrderDto?>(t => t.Result.Items.FirstOrDefault(x => x.OrderNumber.Equals(externalOrderId, StringComparison.OrdinalIgnoreCase)), cancellationToken);
    }

    public async Task<InvoiceOperationResultDto> CreateInvoiceAsync(InvoiceCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_credentials.Username) || string.IsNullOrWhiteSpace(_credentials.Password))
            throw new ProviderValidationException("Hepsiburada fatura gönderimi için X-Hb-Username ve X-Hb-Password zorunludur.");
        if (string.IsNullOrWhiteSpace(request.OrderId))
            throw new ProviderValidationException("Fatura gönderimi için OrderId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.InvoiceUrl))
            throw new ProviderValidationException("Hepsiburada fatura linki için InvoiceUrl zorunludur.");

        var endpoint = $"packages/merchantid/{Uri.EscapeDataString(_credentials.MerchantId)}/packagenumber/{Uri.EscapeDataString(request.OrderId)}/invoice";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, endpoint);

        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_credentials.Username}:{_credentials.Password}"));
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
        httpRequest.Headers.TryAddWithoutValidation("User-Agent", $"{_credentials.MerchantId} - SelfIntegration");

        var now = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        var payload = new
        {
            arrangementDate = now,
            invoiceLink = request.InvoiceUrl,
            rowNumber = request.ExternalInvoiceNumber ?? "1",
            serialNumber = request.ExternalInvoiceNumber ?? "HBINV",
            invoices = new[]
            {
                new
                {
                    arrangementDate = now,
                    invoiceLink = request.InvoiceUrl,
                    orderNumber = request.OrderId,
                    rowNumber = request.ExternalInvoiceNumber ?? "1",
                    serialNumber = request.ExternalInvoiceNumber ?? "HBINV"
                }
            }
        };
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Hepsiburada API kimlik doğrulaması başarısız.");
        if (!response.IsSuccessStatusCode)
            throw new ExternalProviderException($"Hepsiburada invoice endpoint hatası: {(int)response.StatusCode}", response.StatusCode);

        return new InvoiceOperationResultDto
        {
            Success = true,
            ProviderMessage = "Hepsiburada fatura linki başarılı şekilde gönderildi.",
            ExternalInvoiceId = request.ExternalInvoiceNumber,
            DocumentUrl = request.InvoiceUrl
        };
    }

    public Task<InvoiceOperationResultDto> CancelInvoiceAsync(InvoiceCancelRequestDto request, CancellationToken cancellationToken = default)
        => throw new NotSupportedByProviderException("Hepsiburada fatura iptal operasyonu bu adaptörde desteklenmiyor.");

    public Task<InvoiceDocumentResultDto> GetInvoiceDocumentAsync(InvoiceDocumentRequestDto request, CancellationToken cancellationToken = default)
        => throw new NotSupportedByProviderException("Hepsiburada fatura dökümanı operasyonu bu adaptörde desteklenmiyor.");
}
