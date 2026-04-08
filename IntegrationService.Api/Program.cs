using IntegrationService.Api.Infrastructure;
using Microsoft.OpenApi;
using System.Text.Encodings.Web;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Integration Service — Çoklu Provider Sipariş API",
        Version = "v1",
        Description = "Sipariş + fatura uçlarında provider header setini doldurun. Trendyol, Hepsiburada, N11 ve PttAVM desteklenir."
    });
    options.OperationFilter<IntegrationHeadersOperationFilter>();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<IOrderProviderFactory, OrderProviderFactory>();

var trendyolBaseUrl = builder.Configuration["Trendyol:BaseUrl"] ?? "https://apigw.trendyol.com/integration/";
builder.Services.AddHttpClient("Trendyol", client =>
{
    client.BaseAddress = new Uri(trendyolBaseUrl.EndsWith('/') ? trendyolBaseUrl : trendyolBaseUrl + "/");
    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
});

var hepsiburadaBaseUrl = builder.Configuration["Hepsiburada:BaseUrl"] ?? "https://oms-external-sit.hepsiburada.com/";
builder.Services.AddHttpClient("Hepsiburada", client =>
{
    client.BaseAddress = new Uri(hepsiburadaBaseUrl.EndsWith('/') ? hepsiburadaBaseUrl : hepsiburadaBaseUrl + "/");
    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
});

var n11BaseUrl = builder.Configuration["N11:BaseUrl"] ?? "https://api.n11.com/";
builder.Services.AddHttpClient("N11", client =>
{
    client.BaseAddress = new Uri(n11BaseUrl.EndsWith('/') ? n11BaseUrl : n11BaseUrl + "/");
    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
});

var app = builder.Build();

app.UseExceptionHandler();


app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Integration Service v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
