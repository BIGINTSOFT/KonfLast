using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Application.Abstractions;

namespace ERP.Konfeksiyon.Application.Features.Costing;

public static class CostRules
{
    /// <summary>
    /// Maliyet alanlarını (birim maliyet, adet başı maliyet, hareket maliyeti) görebilir mi: maliyet görüntüleme
    /// ya da maliyeti değiştirme yetkisi gerekir. Yoksa bu alanlar boş döner.
    /// </summary>
    public static async Task<bool> CanSeeCostAsync(ICurrentPermissions permissions, CancellationToken cancellationToken) =>
        await permissions.HasAsync(KonfeksiyonPermissions.Costing.View, cancellationToken)
        || await permissions.HasAsync(KonfeksiyonPermissions.Costing.Manage, cancellationToken);
}
