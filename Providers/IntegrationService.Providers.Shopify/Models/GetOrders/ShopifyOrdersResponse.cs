using System.Text.Json.Serialization;

namespace IntegrationService.Providers.Shopify.Models.GetOrders;

/// <summary>GET /admin/api/{version}/orders.json yanıtı. İstek gövdesi yoktur, parametreler query string ile gider.</summary>
internal sealed class ShopifyOrdersResponse
{
    [JsonPropertyName("orders")]
    public List<ShopifyOrderJson>? Orders { get; set; }
}

/// <summary>GET /admin/api/{version}/orders/{id}.json yanıtı.</summary>
internal sealed class ShopifyOrderResponse
{
    [JsonPropertyName("order")]
    public ShopifyOrderJson? Order { get; set; }
}

/// <summary>GET /admin/api/{version}/orders/count.json yanıtı.</summary>
internal sealed class ShopifyOrderCountResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

internal sealed class ShopifyOrderJson
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    /// <summary>Mağazaya görünen sipariş numarası, örn. "#1001".</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("order_number")]
    public long? OrderNumber { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("financial_status")]
    public string? FinancialStatus { get; set; }

    [JsonPropertyName("fulfillment_status")]
    public string? FulfillmentStatus { get; set; }

    [JsonPropertyName("cancelled_at")]
    public DateTimeOffset? CancelledAt { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("subtotal_price")]
    public string? SubtotalPrice { get; set; }

    [JsonPropertyName("total_price")]
    public string? TotalPrice { get; set; }

    [JsonPropertyName("total_tax")]
    public string? TotalTax { get; set; }

    [JsonPropertyName("total_discounts")]
    public string? TotalDiscounts { get; set; }

    [JsonPropertyName("total_shipping_price_set")]
    public ShopifyPriceSetJson? TotalShippingPriceSet { get; set; }

    [JsonPropertyName("customer")]
    public ShopifyCustomerJson? Customer { get; set; }

    [JsonPropertyName("shipping_address")]
    public ShopifyAddressJson? ShippingAddress { get; set; }

    [JsonPropertyName("billing_address")]
    public ShopifyAddressJson? BillingAddress { get; set; }

    [JsonPropertyName("line_items")]
    public List<ShopifyLineItemJson>? LineItems { get; set; }
}

internal sealed class ShopifyPriceSetJson
{
    [JsonPropertyName("shop_money")]
    public ShopifyMoneyJson? ShopMoney { get; set; }
}

internal sealed class ShopifyMoneyJson
{
    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; set; }
}

internal sealed class ShopifyCustomerJson
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

internal sealed class ShopifyAddressJson
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("address1")]
    public string? Address1 { get; set; }

    [JsonPropertyName("address2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>İl/eyalet. Türkiye'de ilçe bilgisi genelde burada veya address2'de gelir.</summary>
    [JsonPropertyName("province")]
    public string? Province { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("zip")]
    public string? Zip { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

internal sealed class ShopifyLineItemJson
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("total_discount")]
    public string? TotalDiscount { get; set; }

    [JsonPropertyName("tax_lines")]
    public List<ShopifyTaxLineJson>? TaxLines { get; set; }
}

internal sealed class ShopifyTaxLineJson
{
    /// <summary>Oran ondalık gelir (0.18 = %18).</summary>
    [JsonPropertyName("rate")]
    public decimal? Rate { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }
}
