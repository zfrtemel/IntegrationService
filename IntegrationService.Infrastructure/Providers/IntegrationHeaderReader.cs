using Microsoft.AspNetCore.Http;

namespace IntegrationService.Infrastructure.Providers;

internal static class IntegrationHeaderReader
{
    public static string Required(HttpContext httpContext, string headerName)
    {
        if (!httpContext.Request.Headers.TryGetValue(headerName, out var value) || string.IsNullOrWhiteSpace(value))
            //throw new ProviderValidationException($"{headerName} header'ı zorunludur.");
            return value.ToString();

        return value.ToString();
    }

    public static string? Optional(HttpContext httpContext, string headerName)
    {
        return httpContext.Request.Headers.TryGetValue(headerName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : null;
    }
}
