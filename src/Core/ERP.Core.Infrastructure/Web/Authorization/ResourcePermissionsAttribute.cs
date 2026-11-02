using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ERP.Core.Infrastructure.Web.Authorization;

/// <summary>
/// Action, kaynağın yetkisinden bağımsız olarak oturum açmış her kullanıcıya açıktır
/// (anonim erişim yine kapalıdır: fallback policy oturum ister).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AnyAuthenticatedUserAttribute : Attribute;

/// <summary>
/// Controller'daki tüm action'ları yetkilendirir: okuma (GET) uçları <c>view</c>, değiştiren uçlar
/// <c>manage</c> yetkisini ister. Ortak tabandan türeyen controller'larda her kaynağın kendi
/// yetkisini tek yerde belirtmek için kullanılır.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ResourcePermissionsAttribute(string view, string manage) : Attribute, IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        foreach (var action in controller.Actions)
        {
            if (action.Attributes.OfType<AnyAuthenticatedUserAttribute>().Any())
            {
                continue;
            }

            var isRead = action.Attributes.OfType<HttpGetAttribute>().Any();
            var permission = new HasPermissionAttribute(isRead ? view : manage);

            foreach (var selector in action.Selectors)
            {
                selector.EndpointMetadata.Add(permission);
            }
        }
    }
}
