namespace IntegrationService.Core.Models.Orders;

public sealed class UnifiedOrderQuery
{
    public int Page { get; init; } = 0;
    public int Size { get; init; } = 50;
    public long? StartDateUnixMs { get; init; }
    public long? EndDateUnixMs { get; init; }
    public string? Status { get; init; }
    public string? OrderNumber { get; init; }
}
