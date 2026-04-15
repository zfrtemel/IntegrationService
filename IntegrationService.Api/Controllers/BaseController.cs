using IntegrationService.Core.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace IntegrationService.Api.Controllers;

public abstract class BaseController : ControllerBase
{
    public APIResult<T> ApiResult<T>(string message, bool status, T? data = default)
        => new APIResult<T>(message, status, data);

    public APIResult<T> ApiResult<T>(string message, bool status, T? data = default, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new APIResult<T>(message, status, data, statusCode);
}
