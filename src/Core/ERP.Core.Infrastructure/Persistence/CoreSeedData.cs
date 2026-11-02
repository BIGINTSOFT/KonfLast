using ERP.Core.Domain.Authorization;
using ERP.Core.Domain.Entities;
using ERP.Core.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Infrastructure.Persistence;

/// <summary>
/// Çekirdeğin başlangıç verisi (roller, çekirdek yetkileri ve rol atamaları, para birimleri). Entity
/// konfigürasyonlarından ayrı tutulur: modül migration'larında çekirdek tablolar modelde bulunur ama
/// onların verisi yalnızca çekirdeğin migration'ına girer.
/// </summary>
public static class CoreSeedData
{
    /// <summary>Hazır gelen para birimleri (sistemde önceden kullanılan kodlar). Id'ler sabittir.</summary>
    private static readonly (int Id, string Code, string Name, string Symbol)[] Currencies =
    [
        (1, "TRY", "Türk lirası", "₺"),
        (2, "USD", "ABD doları", "$"),
        (3, "EUR", "Euro", "€"),
        (4, "GBP", "İngiliz sterlini", "£"),
    ];

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(Roles.All.Select(r => new Role { Id = r.Id, Code = r.Code, Name = r.Name, ModuleKey = r.ModuleKey, IsSystem = true }));

        modelBuilder.Entity<Permission>().HasData(Permissions.All.Select(p => new Permission
        {
            Id = p.Id,
            Code = p.Code,
            Module = p.Module,
            Description = p.Description,
        }));

        var roleIds = Roles.All.ToDictionary(r => r.Code, r => r.Id);
        var permissionIds = Permissions.All.ToDictionary(p => p.Code, p => p.Id);
        modelBuilder.Entity<RolePermission>().HasData(Roles.DefaultPermissions.SelectMany(pair => pair.Value.Select(code => new RolePermission
        {
            RoleId = roleIds[pair.Key],
            PermissionId = permissionIds[code],
        })));

        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Currency>().HasData(Currencies.Select(d => new Currency
        {
            Id = d.Id, Code = d.Code, Name = d.Name, Symbol = d.Symbol, DecimalPlaces = 2, IsActive = true,
            FetchFromTcmb = d.Code != Currency.LocalCode, CreatedAt = seededAt, CreatedBy = "system",
        }));
    }
}
