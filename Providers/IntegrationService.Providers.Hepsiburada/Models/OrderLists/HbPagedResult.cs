namespace IntegrationService.Providers.Hepsiburada.Models.OrderLists;

internal sealed class HbPaymentAwaitingPage
{
    public List<HbPaymentAwaitingLine>? Items { get; set; }
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public int PageCount { get; set; }
}

internal sealed class HbPaymentAwaitingLine
{
    public string? Id { get; set; }
    public string? OrderNumber { get; set; }
    public string? OrderDate { get; set; }
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public string? MerchantSku { get; set; }
    public int? Quantity { get; set; }
}

internal sealed class HbCancelledPage
{
    public List<HbCancelledLine>? Items { get; set; }
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public int PageCount { get; set; }
}

internal sealed class HbCancelledLine
{
    public string? LineItemId { get; set; }
    public string? OrderNumber { get; set; }
    public string? MerchantSku { get; set; }
    public string? Sku { get; set; }
    public int? Quantity { get; set; }
    public string? CancelDate { get; set; }
    public string? CancelReasonCode { get; set; }
    public string? CancelledBy { get; set; }
}

internal sealed class HbPackagePage<T>
{
    public List<T>? Items { get; set; }
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public int PageCount { get; set; }
}

internal sealed class HbPackageRow
{
    public string? Id { get; set; }
    public string? PackageNumber { get; set; }
    public string? OrderNumber { get; set; }
    public List<string>? OrderNumbers { get; set; }
    public string? Barcode { get; set; }
    public string? DeliveredDate { get; set; }
    public string? ShippedDate { get; set; }
    public string? UndeliveredDate { get; set; }
    public string? UndeliveredReason { get; set; }
}

internal sealed class HbMissingInvoicePackage
{
    public string? PackageNumber { get; set; }
    public List<string>? OrderNumbers { get; set; }
    public string? Status { get; set; }
}

internal sealed class HbUnpackedDelivery
{
    public string? PackageNumber { get; set; }
    public string? Barcode { get; set; }
    public string? UnpackedDate { get; set; }
}
