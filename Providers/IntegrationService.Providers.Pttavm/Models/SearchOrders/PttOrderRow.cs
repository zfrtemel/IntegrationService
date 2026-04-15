namespace IntegrationService.Providers.Pttavm.Models.SearchOrders;

internal sealed class PttOrderRow
{
    public string? VergiDaire { get; set; }
    public string? VergiNo { get; set; }
    public double? KargoTutari { get; set; }
    public string? Eposta { get; set; }
    public string? Tckn { get; set; }
    public DateTimeOffset? IslemTarihi { get; set; }
    public string? SiparisNo { get; set; }
    public string? MusteriAdi { get; set; }
    public string? MusteriSoyadi { get; set; }
    public string? SiparisAdresi { get; set; }
    public string? TelefonNo { get; set; }
    public string? SiparisIli { get; set; }
    public string? SiparisIlce { get; set; }
    public string? FaturaMusteriAdi { get; set; }
    public string? FaturaMusteriSoyadi { get; set; }
    public string? FaturaAdresi { get; set; }
    public string? FaturaIli { get; set; }
    public string? FaturaIlce { get; set; }
    public string? FaturaTip { get; set; }
    public List<PttLineRow>? SiparisUrunler { get; set; }
}
