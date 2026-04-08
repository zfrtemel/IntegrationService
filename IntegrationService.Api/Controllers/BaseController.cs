using System.Net;
using IntegrationService.Core.Responses;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.Api.Controllers;

public abstract class BaseController : ControllerBase
{
    protected APIResult<T> ApiResult<T>(string message, bool status, T? data = default)
        => new APIResult<T>(message, status, data);

    protected APIResult<T> ApiResult<T>(string message, bool status, T? data, HttpStatusCode statusCode)
        => new APIResult<T>(message, status, data, statusCode);

    protected IActionResult ApiResponse<T>(APIResult<T> result)
        => new ObjectResult(result) { StatusCode = (int)result.StatusCode };
}
