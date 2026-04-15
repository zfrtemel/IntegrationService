using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IntegrationService.Core.Responses;

public interface IAPIResult<T>
{
    public bool Status { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
}

public class APIResult<T> : IAPIResult<T>, IActionResult
{
    public bool Status { get; set; }
    public string Message { get; set; } = "";
    public T? Data { get; set; }

    [JsonIgnore]
    public HttpStatusCode Code { get; set; } = HttpStatusCode.OK;

    public APIResult(string message, bool status, T? data = default)
    {
        Message = message;
        Status = status;
        Data = data;
    }

    public APIResult(string message, bool status, T? data = default, HttpStatusCode code = HttpStatusCode.OK)
    {
        Message = message;
        Status = status;
        Data = data;
        Code = code;
    }

    public Task ExecuteResultAsync(ActionContext context)
    {
        var is_development = (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production") == "Development";
        try
        {
            return Task.Run(async () =>
            {
                var response = context.HttpContext.Response;
                response.ContentType = "application/json";
                response.StatusCode = (int)Code;

                var result = new
                {
                    Message,
                    Status,
                    Data
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = is_development,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    MaxDepth = 0,
                };

                await response.WriteAsync(JsonSerializer.Serialize<object>(result, options));
            });
        }
        catch (JsonException jsonEx)
        {
            return Task.Run(async () =>
            {
                var response = context.HttpContext.Response;
                response.ContentType = "application/json";
                response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var errorResult = new
                {
                    Message = "Serialize işleminde hata oluştu. Veri serialize edilemiyor.",
                    Status = false,
                    Data = is_development ? jsonEx.Message : null,
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = is_development,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                await response.WriteAsync(JsonSerializer.Serialize(errorResult, options));
            });
        }
        catch (Exception ex)
        {
            return Task.Run(async () =>
            {
                var response = context.HttpContext.Response;
                response.ContentType = "application/json";
                response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var errorResult = new
                {
                    Message = "Response oluşturulurken hata oluştu. Lütfen sistem yöneticisi ile iletişime geçiniz.",
                    Status = false,
                    Data = is_development ? ex.Message : null
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = is_development,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                await response.WriteAsync(JsonSerializer.Serialize(errorResult, options));
            });
        }
    }
}