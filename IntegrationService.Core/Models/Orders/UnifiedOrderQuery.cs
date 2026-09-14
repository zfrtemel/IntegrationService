using System.Text.Json.Serialization;

namespace IntegrationService.Core.Models.Orders;

public sealed class UnifiedOrderQuery
{
    public int Page { get; init; } = 0;
    public int Size { get; init; } = 50;

    /// <summary>Başlangıç tarihi. Örn: 2026-09-01 veya 2026-09-01T00:00:00.</summary>
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>Bitiş tarihi. Örn: 2026-09-14 veya 2026-09-14T23:59:59.</summary>
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>Sağlayıcıya özgü statü filtresi; Hepsiburada entegrasyonunda kullanılabilecek değerler ilgili provider dokümantasyonunda listelenir.</summary>
    public string? Status { get; init; }
    public string? OrderNumber { get; init; }

    [JsonIgnore]
    public long? StartDateUnixMs => StartDate?.ToUnixTimeMilliseconds();

    [JsonIgnore]
    public long? EndDateUnixMs => EndDate?.ToUnixTimeMilliseconds();
}
