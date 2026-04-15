namespace IntegrationService.Providers.Hepsiburada;

/// <summary>
/// Hepsiburada <see cref="IntegrationService.Core.Models.Orders.UnifiedOrderQuery.Status"/> için izin verilen değerler.
/// Her biri HB OMS'te ayrı statü bazlı listeleme endpoint'ine karşılık gelir; tek bir "tüm siparişler" endpoint'i yoktur.
/// </summary>
public static class HepsiburadaOrderStatuses
{
    /// <summary>GET orders/merchantid/{merchantId}/paymentawaiting — ödemesi beklenen kalemler.</summary>
    public const string PaymentAwaiting = nameof(PaymentAwaiting);

    /// <summary>GET orders/merchantid/{merchantId} — ödemesi tamamlanmış (paketlenecek) kalemler.</summary>
    public const string PaymentCompleted = nameof(PaymentCompleted);

    /// <summary>GET orders/merchantid/{merchantId}/cancelled — iptal kalemleri.</summary>
    public const string Cancelled = nameof(Cancelled);

    /// <summary>GET packages/merchantid/{merchantId}/shipped — kargoya verilmiş paketler.</summary>
    public const string Shipped = nameof(Shipped);

    /// <summary>GET packages/merchantid/{merchantId}/delivered — teslim edilmiş paketler.</summary>
    public const string Delivered = nameof(Delivered);

    /// <summary>GET packages/merchantid/{merchantId}/undelivered — teslim edilememiş paketler.</summary>
    public const string Undelivered = nameof(Undelivered);

    /// <summary>GET packages/merchantid/{merchantId}/missing-invoice — faturası yüklenmemiş paketler.</summary>
    public const string MissingInvoice = nameof(MissingInvoice);

    /// <summary>GET packages/merchantid/{merchantId}/status/unpacked — bozulan (unpack) paketler.</summary>
    public const string Unpacked = nameof(Unpacked);

    internal static readonly string[] All =
    [
        PaymentAwaiting,
        PaymentCompleted,
        Cancelled,
        Shipped,
        Delivered,
        Undelivered,
        MissingInvoice,
        Unpacked
    ];
}
