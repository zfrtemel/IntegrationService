using System.Net;

namespace IntegrationService.Core.Responses;

public class APIResult<T>
{
    public APIResult(string message, bool status, T? data = default)
    {
        Message = message;
        Status = status;
        Data = data;
        StatusCode = HttpStatusCode.OK;
    }

    public APIResult(string message, bool status, T? data, HttpStatusCode statusCode)
    {
        Message = message;
        Status = status;
        Data = data;
        StatusCode = statusCode;
    }

    public string Message { get; }
    public bool Status { get; }
    public T? Data { get; }
    public HttpStatusCode StatusCode { get; }
}
