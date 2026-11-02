using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;

namespace ERP.Api.Middleware;

/// <summary>Beklenmeyen hataları loglar ve istemciye stack trace içermeyen güvenli bir yanıt döner.</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // İstemci bağlantıyı kapattı; yazılacak bir yanıt yok.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "İşlenmeyen hata: {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            await ApiResponse.WriteFailureAsync(
                context,
                StatusCodes.Status500InternalServerError,
                ErrorCodes.InternalError,
                "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.");
        }
    }
}
