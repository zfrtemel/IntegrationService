namespace IntegrationService.Core.Providers;

public static class IntegrationHeaderNames
{
    public const string Provider = "X-Integration-Provider";

    public const string TrendyolSupplierId = "X-Trendyol-Supplier-Id";
    public const string TrendyolApiKey = "X-Trendyol-Api-Key";
    public const string TrendyolApiSecret = "X-Trendyol-Api-Secret";
    public const string TrendyolIntegrationLabel = "X-Trendyol-Integration-Label";
    public const string TrendyolStoreFrontCode = "X-Trendyol-Store-Front-Code";

    public const string HepsiburadaMerchantId = "X-Hb-Merchant-Id";
    public const string HepsiburadaUsername = "X-Hb-Username";
    public const string HepsiburadaPassword = "X-Hb-Password";

    public const string N11ApiKey = "X-N11-Api-Key";
    public const string N11ApiSecret = "X-N11-Api-Secret";
    public const string N11MerchantId = "X-N11-Merchant-Id";

    public const string PttavmApiKey = "X-Pttavm-Api-Key";
    public const string PttavmAccessToken = "X-Pttavm-Access-Token";
    public const string PttavmCorrelationId = "X-Pttavm-Correlation-Id";

    public const string TestMerchantId = "X-Test-Merchant-Id";
}
