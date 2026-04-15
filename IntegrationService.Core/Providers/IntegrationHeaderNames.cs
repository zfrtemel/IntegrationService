namespace IntegrationService.Core.Providers;

public static class IntegrationHeaderNames
{
    public const string Provider = "X-Integration-Provider";
    public const string MerchantId = "X-Integration-Merchant-Id";
    public const string SupplierId = "X-Integration-Supplier-Id";
    public const string ApiKey = "X-Integration-Api-Key";
    public const string ApiSecret = "X-Integration-Api-Secret";
    public const string Username = "X-Integration-Username";
    public const string Password = "X-Integration-Password";
    public const string AccessToken = "X-Integration-Access-Token";
    public const string CorrelationId = "X-Integration-Correlation-Id";
    public const string StoreFrontCode = "X-Integration-Store-Front-Code";
}
