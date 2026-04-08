namespace IntegrationService.Core.Models.Orders;

public sealed class UnifiedOrderPageDto
{
    public IReadOnlyList<UnifiedOrderDto> Items { get; init; } = Array.Empty<UnifiedOrderDto>();
    public int Page { get; init; }
    public int Size { get; init; }
    public int TotalPages { get; init; }
    public long TotalElements { get; init; }
}
