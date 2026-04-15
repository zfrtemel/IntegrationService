namespace IntegrationService.Providers.Hepsiburada.Models.OpenLineItems;

internal sealed class HbInvoice
{
    public HbAddress? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? TurkishIdentityNumber { get; set; }
}
