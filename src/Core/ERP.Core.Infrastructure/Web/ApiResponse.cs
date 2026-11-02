using Microsoft.AspNetCore.Http;
using ERP.Core.Application.Common;

namespace ERP.Core.Infrastructure.Web;

public sealed record ApiError(string Code, string Message, IReadOnlyDictionary<string, string[]>? Details = null);

/// <summary>Tüm endpoint'lerin ortak yanıt zarfı.</summary>
public sealed record ApiResponse<T>(bool Success, T? Data, ApiError? Error, string CorrelationId);

public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T data, HttpContext context) =>
        new(true, data, null, context.TraceIdentifier);

    public static ApiResponse<object?> Failure(
        HttpContext context, string code, string message, IReadOnlyDictionary<string, string[]>? details = null) =>
        new(false, null, new ApiError(code, message, details), context.TraceIdentifier);

    public static ApiResponse<object?> Failure(HttpContext context, Error error) =>
        Failure(context, error.Code, error.Message, error.Details);

    public static Task WriteFailureAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? details = null)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(Failure(context, code, message, details), context.RequestAborted);
    }

    public static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError,
    };
}
