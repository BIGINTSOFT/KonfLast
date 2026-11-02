using ERP.Core.Application.Abstractions;
using ERP.Core.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ERP.Core.Infrastructure.Web.Authorization;

/// <summary>
/// Endpoint'i permission ile korur: [HasPermission(Permissions.User.View)].
/// Birden fazla verilirse HERHANGİ BİRİ yeterlidir. Hepsinin gerekmesi için attribute tekrarlanır.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute(params string[] anyOf)
    : AuthorizeAttribute(PolicyPrefix + string.Join(Separator, anyOf))
{
    public const string PolicyPrefix = "perm:";
    public const char Separator = '|';
}

public sealed record PermissionRequirement(IReadOnlyList<string> AnyOf) : IAuthorizationRequirement;

/// <summary>"perm:X" adlı policy'leri her permission için ayrı kayıt gerektirmeden üretir.</summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return await base.GetPolicyAsync(policyName);
        }

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(
                policyName[HasPermissionAttribute.PolicyPrefix.Length..].Split(HasPermissionAttribute.Separator)))
            .Build();
    }
}

public sealed class PermissionAuthorizationHandler(IPermissionService permissionService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var roles = context.User.FindAll(JwtTokenService.RoleClaimType).Select(c => c.Value);
        var permissions = await permissionService.GetPermissionsAsync(roles);

        if (requirement.AnyOf.Any(permissions.Contains))
        {
            context.Succeed(requirement);
        }
    }
}
