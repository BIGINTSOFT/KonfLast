using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ERP.Core.Infrastructure.Web;

/// <summary>
/// Sorgu parametresi / route'tan gelen enum değerlerini JSON'daki biçimle (UPPER_SNAKE, örn.
/// <c>IN_PRODUCTION</c>) okur. Varsayılan bağlayıcı yalnızca C# adını (InProduction) kabul eder; böylece
/// API'nin döndürdüğü durum değeri filtre olarak aynen geri gönderilebilir.
/// </summary>
public sealed class SnakeUpperEnumModelBinder(Type enumType) : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);

        var raw = value.FirstValue;
        if (string.IsNullOrWhiteSpace(raw))
        {
            // Boş değer: nullable alan için "verilmedi" demektir; zorunlu alanı doğrulama yakalar.
            return Task.CompletedTask;
        }

        // Sayısal değerler kabul edilmez: enum'un iç değeri sözleşmenin parçası değildir.
        var name = raw.Trim().Replace("_", string.Empty, StringComparison.Ordinal);
        if (!char.IsDigit(name[0]) && name[0] != '-'
            && Enum.TryParse(enumType, name, ignoreCase: true, out var parsed)
            && Enum.IsDefined(enumType, parsed!))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"'{raw}' geçerli bir değer değil.");
        }

        return Task.CompletedTask;
    }
}

public sealed class SnakeUpperEnumModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return type.IsEnum ? new SnakeUpperEnumModelBinder(type) : null;
    }
}
