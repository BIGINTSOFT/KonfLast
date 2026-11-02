using ERP.Core.Application.Abstractions;
using ERP.Core.Domain.Authorization;

namespace ERP.Core.Infrastructure.Modules;

public sealed class ActivePermissions(ErpModuleCatalog modules) : IActivePermissions
{
    private readonly HashSet<string> _codes = Permissions.All
        .Concat(modules.Modules.SelectMany(m => m.Permissions))
        .Select(p => p.Code)
        .ToHashSet(StringComparer.Ordinal);

    private readonly Dictionary<string, string> _moduleOf = modules.Modules
        .SelectMany(m => m.Permissions.Select(p => (p.Code, m.Key)))
        .ToDictionary(x => x.Code, x => x.Key, StringComparer.Ordinal);

    public bool Contains(string code) => _codes.Contains(code);

    public string? ModuleOf(string code) => _moduleOf.GetValueOrDefault(code);
}
