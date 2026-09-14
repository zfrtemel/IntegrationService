using IntegrationService.Core.Providers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IntegrationService.Infrastructure.Web;

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

        Header(IntegrationHeaderNames.Provider, "Zorunlu: Trendyol, Hepsiburada, N11, Pttavm veya Shopify", required: true);
        //Header(IntegrationHeaderNames.ShopDomain, "Shopify için zorunlu, örn. my-store.myshopify.com.");
        //Header(IntegrationHeaderNames.ApiVersion, "Shopify: boş bırakılırsa varsayılan Admin API sürümü kullanılır.");
        //Header(IntegrationHeaderNames.SupplierId, "Trendyol için zorunlu (sayı). Diğer provider’larda kullanılmaz.");
        //Header(IntegrationHeaderNames.MerchantId, "Hepsiburada ve N11 için zorunlu. Trendyol/Pttavm’de kullanılmaz.");
        //Header(IntegrationHeaderNames.ApiKey, "Trendyol, N11 ve Pttavm için zorunlu.");
        //Header(IntegrationHeaderNames.ApiSecret, "Trendyol ve N11 için zorunlu.");
        //Header(IntegrationHeaderNames.Username, "Hepsiburada sipariş/fatura için zorunlu (Basic auth).");
        //Header(IntegrationHeaderNames.Password, "Hepsiburada sipariş/fatura için zorunlu (Basic auth).");
        //Header(IntegrationHeaderNames.AccessToken, "Pttavm için zorunlu.");
        //Header(IntegrationHeaderNames.CorrelationId, "Pttavm: boş bırakılırsa sunucu bir değer üretir.");
        //Header(IntegrationHeaderNames.StoreFrontCode, "Trendyol: boşsa appsettings (TrendyolProvider:DefaultStoreFrontCode), yoksa TR.");
    }
}
