using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using IntegrationService.Core.Exceptions;
using IntegrationService.Core.Responses;
using Microsoft.AspNetCore.Diagnostics;

namespace IntegrationService.Api.Infrastructure;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOutput = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, message, logAsError) = MapException(exception);

        if (logAsError)
            _logger.LogError(exception, "İşlenmeyen hata");
        else
            _logger.LogInformation("{Message}", message);

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        var payload = new APIResult<object?>(message, false, null, statusCode);
        await httpContext.Response.WriteAsJsonAsync(payload, JsonOutput, cancellationToken);
        return true;
    }

    private static (HttpStatusCode StatusCode, string Message, bool LogAsError) MapException(Exception exception)
    {
        return exception switch
        {
            ProviderValidationException e => (HttpStatusCode.BadRequest, e.Message, false),
            ProviderNotSupportedException e => (HttpStatusCode.BadRequest, e.Message, false),
            NotSupportedByProviderException e => ((HttpStatusCode)422, e.Message, false),
            UnauthorizedAccessException e => (HttpStatusCode.Unauthorized, e.Message, false),
            ExternalProviderException e => (MapProviderHttpStatus(e.StatusCode), e.Message, false),
            HttpRequestException e => (HttpStatusCode.BadGateway, "Harici servise erişilemedi.", true),
            JsonException e => (HttpStatusCode.BadGateway, "Harici servisten gelen veri işlenemedi.", true),
            _ => (HttpStatusCode.InternalServerError, "Beklenmeyen bir hata oluştu.", true)
        };
    }

    private static HttpStatusCode MapProviderHttpStatus(HttpStatusCode? status)
    {
        if (!status.HasValue)
            return HttpStatusCode.BadGateway;

        var code = (int)status.Value;
        if (code is >= 500)
            return HttpStatusCode.BadGateway;
        if (code == (int)HttpStatusCode.TooManyRequests)
            return HttpStatusCode.TooManyRequests;
        if (code is >= 400)
            return HttpStatusCode.BadGateway;

        return HttpStatusCode.BadGateway;
    }
}
