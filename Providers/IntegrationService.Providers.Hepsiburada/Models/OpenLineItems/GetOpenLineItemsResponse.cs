namespace IntegrationService.Providers.Hepsiburada.Models.OpenLineItems;

internal sealed class HbOpenLineItemsPage
{
    public List<HbLineItem>? Items { get; set; }
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public int PageCount { get; set; }
}
