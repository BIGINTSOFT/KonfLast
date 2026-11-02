using ERP.Core.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Core.Infrastructure.Web;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> FromResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(ApiResponse.Success(result.Value, HttpContext)) : Failure(result.Error!);

    protected ActionResult<ApiResponse<object?>> FromResult(Result result) =>
        result.IsSuccess ? Ok(ApiResponse.Success<object?>(null, HttpContext)) : Failure(result.Error!);

    private ObjectResult Failure(Error error) =>
        StatusCode(error.Type.ToStatusCode(), ApiResponse.Failure(HttpContext, error));
}
