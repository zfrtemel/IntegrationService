namespace IntegrationService.Providers.Hepsiburada.Models.OrderDetail;

internal sealed class HbOrderDetailResponse
{
    public string? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string? OrderDate { get; set; }
    public string? CreatedDate { get; set; }
    public string? PaymentStatus { get; set; }
    public HbDetailCustomer? Customer { get; set; }
    public HbDetailAddress? DeliveryAddress { get; set; }
    public HbDetailInvoice? Invoice { get; set; }
    public List<HbDetailLine>? Items { get; set; }
}

internal sealed class HbDetailCustomer
{
    public string? Name { get; set; }
    public string? CustomerId { get; set; }
}

internal sealed class HbDetailAddress
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Town { get; set; }
    public string? District { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public string? Email { get; set; }
}

internal sealed class HbDetailInvoice
{
    public HbDetailAddress? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? TurkishIdentityNumber { get; set; }
}

internal sealed class HbDetailLine
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public string? MerchantSKU { get; set; }
    public int? Quantity { get; set; }
    public string? Status { get; set; }
    public string? OrderNumber { get; set; }
    public string? OrderDate { get; set; }
    public string? PackageNumber { get; set; }
    public HbDetailMoney? UnitPrice { get; set; }
    public HbDetailMoney? TotalPrice { get; set; }
    public decimal? VatRate { get; set; }
    public HbDetailInvoice? Invoice { get; set; }
    public HbDetailAddress? ShippingAddress { get; set; }
    public string? CustomerName { get; set; }
}

internal sealed class HbDetailMoney
{
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
}
