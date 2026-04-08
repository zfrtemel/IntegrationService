using IntegrationService.Core.Providers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IntegrationService.Api.Infrastructure;

public sealed class IntegrationHeadersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<IOpenApiParameter>();

        void Header(string name, string description, bool required = false)
        {
            if (operation.Parameters.Any(p => p is OpenApiParameter op && op.Name == name && op.In == ParameterLocation.Header))
                return;

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = name,
                In = ParameterLocation.Header,
                Required = required,
                Description = description,
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            });
        }

        Header(IntegrationHeaderNames.Provider, "Zorunlu: Trendyol, Hepsiburada, N11 veya Pttavm", required: true);
        Header(IntegrationHeaderNames.TrendyolSupplierId, "Trendyol: SupplierId (path parametresi)");
        Header(IntegrationHeaderNames.TrendyolApiKey, "Trendyol: API Key");
        Header(IntegrationHeaderNames.TrendyolApiSecret, "Trendyol: API Secret");
        Header(IntegrationHeaderNames.TrendyolStoreFrontCode, "Trendyol: mağaza kodu (boşsa API tarafında TR)");
        Header(IntegrationHeaderNames.TrendyolIntegrationLabel, "Trendyol: User-Agent etiketi (boşsa SelfIntegration)");
        Header(IntegrationHeaderNames.HepsiburadaMerchantId, "Hepsiburada: MerchantId");
        Header(IntegrationHeaderNames.HepsiburadaUsername, "Hepsiburada: Basic Auth Username (opsiyonel placeholder)");
        Header(IntegrationHeaderNames.HepsiburadaPassword, "Hepsiburada: Basic Auth Password (opsiyonel placeholder)");
        Header(IntegrationHeaderNames.N11ApiKey, "N11: Api Key");
        Header(IntegrationHeaderNames.N11ApiSecret, "N11: Api Secret");
        Header(IntegrationHeaderNames.N11MerchantId, "N11: MerchantId (opsiyonel)");
        Header(IntegrationHeaderNames.PttavmApiKey, "PttAVM: Api-Key");
        Header(IntegrationHeaderNames.PttavmAccessToken, "PttAVM: access-token");
        Header(IntegrationHeaderNames.PttavmCorrelationId, "PttAVM: X-Correlation-Id (opsiyonel, boşsa üretilir)");
    }
}
