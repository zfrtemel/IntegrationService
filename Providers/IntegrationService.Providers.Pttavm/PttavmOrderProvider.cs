using IntegrationService.Core.Contracts;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Models.Invoices;
using IntegrationService.Core.Models.Orders;
using IntegrationService.Core.Models.Providers;
using IntegrationService.Core.Providers;

namespace IntegrationService.Providers.Pttavm;

public sealed class PttavmOrderProvider : IOrderProvider, IInvoiceProvider
{
    private readonly PttavmCredentials _credentials;

    public PttavmOrderProvider(PttavmCredentials credentials)
    {
        _credentials = credentials;
        _credentials.Validate();
    }

    public IntegrationProviderType Provider => IntegrationProviderType.Pttavm;

    public ProviderCapabilities Capabilities => new()
    {
        SupportsOrderBillingFields = true,
        SupportsInvoiceCreate = true,
        SupportsInvoiceCancel = false,
        SupportsInvoiceDownload = false
    };

    public Task<UnifiedOrderPageDto> GetOrdersAsync(UnifiedOrderQuery query, CancellationToken cancellationToken = default)
    {
        var order = new UnifiedOrderDto
        {
            OrderId = "ptt-1001",
            OrderNumber = "PTT-2026-1001",
            Marketplace = Provider,
            Status = "gonderilmiş",
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-2),
            Customer = new OrderCustomerDto { FullName = "Pttavm Müşteri", Email = "buyer@pttavm.example" },
            Amounts = new OrderAmountSummaryDto { Subtotal = 120m, Shipping = 10m, GrandTotal = 130m, TaxTotal = 20m, Currency = "TRY" },
            ShippingAddress = new OrderAddressDto { FullAddress = "PttAVM Sipariş Adresi", City = "Ankara", District = "Çankaya", CountryCode = "TR" },
            BillingInfo = new OrderBillingInfoDto
            {
                InvoiceType = "Corporate",
                BillingFullNameOrCompany = "Pttavm Kurumsal",
                TaxOffice = "Ankara",
                TaxNumber = "1231231231",
                InvoiceAvailable = true,
                BillingAddress = new OrderAddressDto { FullAddress = "PttAVM Fatura Adresi", City = "Ankara", District = "Çankaya", CountryCode = "TR" }
            },
            Lines =
            [
                new UnifiedOrderLineDto { LineId = "1266416", Sku = "PTT-SKU-1", Name = "Kedi Kumu", Qty = 1, UnitPrice = 80m, LineTotal = 80m, TaxRate = 20 },
                new UnifiedOrderLineDto { LineId = "1266417", Sku = "PTT-SKU-2", Name = "Kedi Maması", Qty = 1, UnitPrice = 50m, LineTotal = 50m, TaxRate = 10 }
            ]
        };

        return Task.FromResult(new UnifiedOrderPageDto
        {
            Items = [order],
            Page = query.Page,
            Size = query.Size,
            TotalElements = 1,
            TotalPages = 1
        });
    }

    public async Task<UnifiedOrderDto?> GetOrderDetailAsync(string orderIdOrNumber, CancellationToken cancellationToken = default)
    {
        var page = await GetOrdersAsync(new UnifiedOrderQuery(), cancellationToken);
        return page.Items.FirstOrDefault(x =>
            x.OrderId.Equals(orderIdOrNumber, StringComparison.OrdinalIgnoreCase) ||
            x.OrderNumber.Equals(orderIdOrNumber, StringComparison.OrdinalIgnoreCase));
    }

    public Task<InvoiceOperationResultDto> CreateInvoiceAsync(InvoiceCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.LineItemIds.Count == 0)
            throw new ProviderValidationException("PttAVM fatura gönderimi için lineItemIds alanı zorunludur.");

        if (string.IsNullOrWhiteSpace(request.PdfBase64) && string.IsNullOrWhiteSpace(request.InvoiceUrl))
            throw new ProviderValidationException("PttAVM fatura gönderimi için PdfBase64 veya InvoiceUrl alanlarından biri zorunludur.");

        return Task.FromResult(new InvoiceOperationResultDto
        {
            Success = true,
            ProviderMessage = "PttAVM public sözleşmesine göre invoice create request formatı kabul edildi (placeholder execution).",
            ExternalInvoiceId = $"ptt-inv-{request.OrderId}"
        });
    }

    public Task<InvoiceOperationResultDto> CancelInvoiceAsync(InvoiceCancelRequestDto request, CancellationToken cancellationToken = default)
        => throw new NotSupportedByProviderException("PttAVM public dokümanda fatura iptal endpointi doğrulanamadığı için placeholder kullanılıyor.");

    public Task<InvoiceDocumentResultDto> GetInvoiceDocumentAsync(InvoiceDocumentRequestDto request, CancellationToken cancellationToken = default)
        => throw new NotSupportedByProviderException("PttAVM public dokümanda fatura döküman endpointi doğrulanamadığı için placeholder kullanılıyor.");
}
