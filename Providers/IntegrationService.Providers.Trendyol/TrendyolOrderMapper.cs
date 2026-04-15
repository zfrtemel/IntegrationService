using IntegrationService.Core.Models.Orders;
using IntegrationService.Core.Providers;
using IntegrationService.Providers.Trendyol.Models.GetOrders;
using System.Text.Json;

namespace IntegrationService.Providers.Trendyol;

internal static class TrendyolOrderMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static UnifiedOrderPageDto ToPageDto(TrendyolOrdersPageResponse response)
    {
        var items = (response.Content ?? [])
            .Select(ToListItem)
            .ToList();

        return new UnifiedOrderPageDto
        {
            Items = items,
            Page = response.Page,
            Size = response.Size,
            TotalPages = response.TotalPages,
            TotalElements = response.TotalElements
        };
    }

    public static UnifiedOrderDto ToListItem(TrendyolShipmentPackageJson p)
    {
        var packageId = (p.ShipmentPackageId ?? p.Id ?? 0).ToString();
        var name = CombineName(p.CustomerFirstName, p.CustomerLastName);
        return new UnifiedOrderDto
        {
            OrderId = packageId,
            OrderNumber = p.OrderNumber ?? string.Empty,
            Marketplace = IntegrationProviderType.Trendyol,
            Status = p.Status ?? p.ShipmentPackageStatus,
            Amounts = new OrderAmountSummaryDto
            {
                Subtotal = ToDecimal(p.PackageGrossAmount ?? p.GrossAmount),
                GrandTotal = ToDecimal(p.PackageGrossAmount ?? p.GrossAmount),
                DiscountTotal = ToDecimal(p.PackageTotalDiscount),
                Currency = string.IsNullOrWhiteSpace(p.CurrencyCode) ? "TRY" : p.CurrencyCode!
            },
            CreatedAt = ToDateTimeOffset(p.OrderDate),
            Customer = new OrderCustomerDto
            {
                FullName = name,
                Email = p.CustomerEmail
            },
            ShippingAddress = new OrderAddressDto
            {
                FullAddress = p.ShipmentAddress?.FullAddress,
                City = p.ShipmentAddress?.City,
                District = p.ShipmentAddress?.District,
                CountryCode = p.ShipmentAddress?.CountryCode,
                PostalCode = p.ShipmentAddress?.PostalCode
            },
            BillingInfo = new OrderBillingInfoDto
            {
                InvoiceType = string.IsNullOrWhiteSpace(p.InvoiceAddress?.TaxNumber) ? "Individual" : "Corporate",
                BillingFullNameOrCompany = p.InvoiceAddress?.FullName ?? name,
                TaxOffice = p.InvoiceAddress?.TaxOffice,
                TaxNumber = p.InvoiceAddress?.TaxNumber,
                BillingAddress = new OrderAddressDto
                {
                    FullAddress = p.InvoiceAddress?.FullAddress,
                    City = p.InvoiceAddress?.City,
                    District = p.InvoiceAddress?.District,
                    CountryCode = p.InvoiceAddress?.CountryCode,
                    PostalCode = p.InvoiceAddress?.PostalCode
                },
                InvoiceAvailable = !string.IsNullOrWhiteSpace(p.InvoiceAddress?.FullAddress)
            },
            Lines = (p.Lines ?? []).Select(ToLineDto).ToList()
        };
    }

    public static UnifiedOrderDto ToDetail(TrendyolShipmentPackageJson p)
    {
        return ToListItem(p);
    }

    private static UnifiedOrderLineDto ToLineDto(TrendyolLineJson line)
    {
        var sku = line.MerchantSku ?? line.StockCode ?? line.Sku ?? string.Empty;
        var qty = line.Quantity ?? 0;
        var unit = ToDecimal(line.LineUnitPrice ?? line.Price);
        var total = line.LineGrossAmount is not null
            ? ToDecimal(line.LineGrossAmount)
            : line.Amount is not null
                ? ToDecimal(line.Amount)
                : unit * qty;

        return new UnifiedOrderLineDto
        {
            LineId = line.LineId?.ToString() ?? line.Id?.ToString() ?? string.Empty,
            Sku = sku,
            Name = line.ProductName ?? sku,
            Qty = qty,
            UnitPrice = unit,
            LineTotal = total,
            TaxRate = ToDecimal(line.VatRate)
        };
    }

    private static string? CombineName(string? first, string? last)
    {
        var a = first?.Trim();
        var b = last?.Trim();
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
            return null;
        return string.Join(' ', new[] { a, b }.Where(s => !string.IsNullOrEmpty(s)));
    }

    private static decimal ToDecimal(double? v) => v.HasValue ? (decimal)v.Value : 0m;

    private static DateTimeOffset? ToDateTimeOffset(long? unixMs)
    {
        if (!unixMs.HasValue || unixMs <= 0)
            return null;
        return DateTimeOffset.FromUnixTimeMilliseconds(unixMs.Value);
    }

    internal static JsonSerializerOptions SerializerOptions => JsonOptions;
}
