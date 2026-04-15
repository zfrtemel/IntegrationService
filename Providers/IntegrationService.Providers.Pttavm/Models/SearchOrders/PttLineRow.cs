namespace IntegrationService.Providers.Pttavm.Models.SearchOrders;

internal sealed class PttLineRow
{
    public int? LineItemId { get; set; }
    public string? SiparisDurumu { get; set; }
    public string? UrunAdi { get; set; }
    public string? UrunKodu { get; set; }
    public double? KdvOrani { get; set; }
    public double? KdvHaricTutar { get; set; }
    public double? KdvHaricToplamTutar { get; set; }
    public double? KdvDahilToplamTutar { get; set; }
    public int? ToplamIslemAdedi { get; set; }
}
