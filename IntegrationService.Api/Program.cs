using IntegrationService.Application.DependencyInjection;
using IntegrationService.Infrastructure.DependencyInjection;
using IntegrationService.Infrastructure.Web;
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
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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
