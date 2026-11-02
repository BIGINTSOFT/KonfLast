using System.Text.Json;
using ERP.Core.Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERP.Core.Infrastructure.Web;

/// <summary>
/// Action argümanları için kayıtlı bir FluentValidation validator'ı varsa çalıştırır;
/// hata durumunda action'a girmeden standart VALIDATION_ERROR yanıtı döner.
/// </summary>
public sealed class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public const string ValidationMessage = "Gönderilen veriler geçersiz.";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                var details = result.Errors
                    .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                context.Result = new BadRequestObjectResult(ApiResponse.Failure(
                    context.HttpContext, ErrorCodes.ValidationError, ValidationMessage, details));
                return;
            }
        }

        await next();
    }
}
