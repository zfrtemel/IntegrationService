using System.Text.Json.Serialization;

namespace IntegrationService.Providers.N11;

internal sealed class N11ShipmentPackagesResponse
{
    [JsonPropertyName("pageCount")]
    public int? PageCount { get; set; }

    [JsonPropertyName("totalPages")]
    public int? TotalPages { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("size")]
    public int? Size { get; set; }

    [JsonPropertyName("content")]
    public List<N11ShipmentPackage>? Content { get; set; }
}

internal sealed class N11ShipmentPackage
{
    [JsonPropertyName("orderNumber")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("customerEmail")]
    public string? CustomerEmail { get; set; }

    [JsonPropertyName("customerfullName")]
    public string? CustomerFullName { get; set; }

    [JsonPropertyName("taxId")]
    public string? TaxId { get; set; }

    [JsonPropertyName("taxOffice")]
    public string? TaxOffice { get; set; }

    [JsonPropertyName("tcIdentityNumber")]
    public string? TcIdentityNumber { get; set; }

    [JsonPropertyName("lastModifiedDate")]
    public long? LastModifiedDate { get; set; }

    [JsonPropertyName("agreedDeliveryDate")]
    public long? AgreedDeliveryDate { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal? TotalAmount { get; set; }

    [JsonPropertyName("totalDiscountAmount")]
    public decimal? TotalDiscountAmount { get; set; }

    [JsonPropertyName("shipmentPackageStatus")]
    public string? ShipmentPackageStatus { get; set; }

    [JsonPropertyName("billingAddress")]
    public N11Address? BillingAddress { get; set; }

    [JsonPropertyName("shippingAddress")]
    public N11Address? ShippingAddress { get; set; }

    [JsonPropertyName("lines")]
    public List<N11Line>? Lines { get; set; }
}

internal sealed class N11Address
{
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("district")]
    public string? District { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("gsm")]
    public string? Gsm { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("taxId")]
    public string? TaxId { get; set; }

    [JsonPropertyName("taxHouse")]
    public string? TaxHouse { get; set; }

    [JsonPropertyName("invoiceType")]
    public int? InvoiceType { get; set; }

    [JsonPropertyName("tcId")]
    public string? TcId { get; set; }
}

internal sealed class N11Line
{
    [JsonPropertyName("orderLineId")]
    public long? OrderLineId { get; set; }

    [JsonPropertyName("stockCode")]
    public string? StockCode { get; set; }

    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("sellerInvoiceAmount")]
    public decimal? SellerInvoiceAmount { get; set; }

    [JsonPropertyName("totalSellerDiscountPrice")]
    public decimal? TotalSellerDiscountPrice { get; set; }

    [JsonPropertyName("vatRate")]
    public decimal? VatRate { get; set; }

    [JsonPropertyName("orderItemLineItemStatusName")]
    public string? OrderItemLineItemStatusName { get; set; }
}
