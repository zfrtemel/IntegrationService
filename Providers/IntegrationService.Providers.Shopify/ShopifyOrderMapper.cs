using IntegrationService.Core.Models.Orders;
using IntegrationService.Core.Providers;
using IntegrationService.Providers.Shopify.Models.GetOrders;
using System.Globalization;
using System.Text.Json;

namespace IntegrationService.Providers.Shopify;

internal static class ShopifyOrderMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static UnifiedOrderPageDto ToPageDto(ShopifyOrdersResponse response, int page, int size, int? totalElements)
    {
        var items = (response.Orders ?? [])
            .Select(ToListItem)
            .ToList();

        // Shopify cursor tabanlı sayfalar; toplam sayı ayrı bir count çağrısından gelir.
        var total = totalElements ?? items.Count;
        var totalPages = size > 0 ? (int)Math.Ceiling(total / (double)size) : 0;

        return new UnifiedOrderPageDto
        {
            Items = items,
            Page = page,
            Size = size,
            TotalPages = totalPages,
            TotalElements = total
        };
    }

    public static UnifiedOrderDto ToListItem(ShopifyOrderJson o)
    {
        var customerName = CombineName(o.Customer?.FirstName, o.Customer?.LastName)
            ?? CombineName(o.ShippingAddress?.FirstName, o.ShippingAddress?.LastName);
        var currency = string.IsNullOrWhiteSpace(o.Currency) ? "TRY" : o.Currency!;
        var billing = o.BillingAddress;
        var billingName = billing?.Name ?? CombineName(billing?.FirstName, billing?.LastName) ?? customerName;

        return new UnifiedOrderDto
        {
            OrderId = o.Id?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            OrderNumber = o.Name ?? o.OrderNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            Marketplace = IntegrationProviderType.Shopify,
            Status = ResolveStatus(o),
            CreatedAt = o.CreatedAt,
            Customer = new OrderCustomerDto
            {
                FullName = customerName,
                Email = o.Customer?.Email ?? o.Email,
                PhoneMasked = MaskPhone(o.Customer?.Phone ?? o.Phone ?? o.ShippingAddress?.Phone)
            },
            Amounts = new OrderAmountSummaryDto
            {
                Subtotal = ToDecimal(o.SubtotalPrice),
                Shipping = ToDecimal(o.TotalShippingPriceSet?.ShopMoney?.Amount),
                DiscountTotal = ToDecimal(o.TotalDiscounts),
                GrandTotal = ToDecimal(o.TotalPrice),
                TaxTotal = ToDecimal(o.TotalTax),
                Currency = currency
            },
            ShippingAddress = ToAddressDto(o.ShippingAddress),
            BillingInfo = new OrderBillingInfoDto
            {
                InvoiceType = string.IsNullOrWhiteSpace(billing?.Company) ? "Individual" : "Corporate",
                BillingFullNameOrCompany = string.IsNullOrWhiteSpace(billing?.Company) ? billingName : billing!.Company,
                BillingAddress = billing is null ? null : ToAddressDto(billing),
                IsDifferentBillingAddress = IsDifferentAddress(o.ShippingAddress, billing),
                InvoiceAvailable = billing is not null
            },
            Lines = (o.LineItems ?? []).Select(ToLineDto).ToList()
        };
    }

    public static UnifiedOrderDto ToDetail(ShopifyOrderJson o) => ToListItem(o);

    private static UnifiedOrderLineDto ToLineDto(ShopifyLineItemJson line)
    {
        var qty = line.Quantity ?? 0;
        var unit = ToDecimal(line.Price);
        var discount = ToDecimal(line.TotalDiscount);

        return new UnifiedOrderLineDto
        {
            LineId = line.Id?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            Sku = line.Sku ?? string.Empty,
            Name = line.Title ?? line.Name ?? line.Sku ?? string.Empty,
            Qty = qty,
            UnitPrice = unit,
            LineTotal = (unit * qty) - discount,
            // Shopify oranı ondalık verir (0.18); diğer provider'larla hizalamak için yüzdeye çeviriyoruz.
            TaxRate = line.TaxLines?.FirstOrDefault()?.Rate is { } rate ? rate * 100m : null
        };
    }

    private static OrderAddressDto ToAddressDto(ShopifyAddressJson? a)
    {
        if (a is null)
            return new OrderAddressDto();

        var full = string.Join(' ', new[] { a.Address1, a.Address2 }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim()));

        return new OrderAddressDto
        {
            FullAddress = string.IsNullOrWhiteSpace(full) ? null : full,
            City = a.Province ?? a.City,
            District = a.Province is null ? null : a.City,
            CountryCode = a.CountryCode,
            PostalCode = a.Zip
        };
    }

    private static string ResolveStatus(ShopifyOrderJson o)
    {
        if (o.CancelledAt.HasValue)
            return "cancelled";
        if (!string.IsNullOrWhiteSpace(o.FulfillmentStatus))
            return o.FulfillmentStatus!;
        return string.IsNullOrWhiteSpace(o.FinancialStatus) ? "unfulfilled" : o.FinancialStatus!;
    }

    private static bool? IsDifferentAddress(ShopifyAddressJson? shipping, ShopifyAddressJson? billing)
    {
        if (shipping is null || billing is null)
            return null;

        return !string.Equals(shipping.Address1?.Trim(), billing.Address1?.Trim(), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(shipping.Zip?.Trim(), billing.Zip?.Trim(), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(shipping.City?.Trim(), billing.City?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var digits = phone.Where(char.IsDigit).ToArray();
        if (digits.Length <= 4)
            return new string('*', digits.Length);

        return new string('*', digits.Length - 4) + new string(digits[^4..]);
    }

    private static string? CombineName(string? first, string? last)
    {
        var a = first?.Trim();
        var b = last?.Trim();
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
            return null;
        return string.Join(' ', new[] { a, b }.Where(s => !string.IsNullOrEmpty(s)));
    }

    /// <summary>Shopify parasal alanları string döner ("123.45"), her zaman invariant kültürle çözülür.</summary>
    private static decimal ToDecimal(string? value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;

    internal static JsonSerializerOptions SerializerOptions => JsonOptions;
}
