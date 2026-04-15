namespace IntegrationService.Providers.Hepsiburada.Models.OpenLineItems;

internal sealed class HbLineItem
{
    public string? Id { get; set; }
    public string? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string? PackageNumber { get; set; }
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public string? MerchantSku { get; set; }
    public string? Status { get; set; }
    public string? OrderDate { get; set; }
    public int? Quantity { get; set; }
    public string? CustomerName { get; set; }
    public HbAddress? ShippingAddress { get; set; }
    public HbInvoice? Invoice { get; set; }
    public HbMoney? TotalPrice { get; set; }
    public HbMoney? UnitPrice { get; set; }
    public decimal? VatRate { get; set; }
}
