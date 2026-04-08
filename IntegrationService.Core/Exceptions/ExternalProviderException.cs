using System.Net;

namespace IntegrationService.Core.Exceptions;

public sealed class ExternalProviderException : Exception
{
    public ExternalProviderException(string message, HttpStatusCode? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }
}
