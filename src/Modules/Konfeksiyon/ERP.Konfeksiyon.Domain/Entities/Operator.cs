using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Domain.Authorization;
using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

public class Operator : SoftDeletableEntity, IActivatable
{
    public string OperatorCode { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    /// <summary>Operatörün mola grubu (yoksa yalnızca herkese açık molalara çıkar).</summary>
    public int? BreakGroupId { get; set; }
    public BreakGroup? BreakGroup { get; set; }

    /// <summary>Sahadaki görevi (operatör, bant şefi, bakım operatörü).</summary>
    public StaffRole Role { get; set; } = StaffRole.Operator;

    /// <summary>Kişiye özel ek yetki: görevi vermese de ara verebilir.</summary>
    public bool CanPause { get; set; }

    /// <summary>Kişiye özel ek yetki: görevi vermese de arıza bildirebilir.</summary>
    public bool CanReportBreakdown { get; set; }

    /// <summary>
    /// Personel bu saha işlemini yapmaya (ya da kartıyla onaylamaya) yetkili mi? Ortak yetkiler görevden
    /// gelir; ara ve arıza bildirme kişiye ayrıca da verilebilir. Arıza kapatma yalnızca görevle verilir.
    /// </summary>
    public bool Allows(FloorAction action, StaffRoleSetting roleSetting) =>
        roleSetting.Allows(action) || action switch
        {
            FloorAction.Pause => CanPause,
            FloorAction.ReportBreakdown => CanReportBreakdown,
            _ => false,
        };
}

/// <summary>
/// Bir görevin (operatör, bant şefi, bakım operatörü) tabletteki ortak yetkileri. Ayrı saklanmaz: görevin
/// karşılık geldiği sistem rolüne verilen saha yetkilerinden (Floor.*) okunur.
/// </summary>
public sealed class StaffRoleSetting
{
    /// <summary>Görev yetkilerini belirleyen saha yetkileri.</summary>
    public static readonly IReadOnlyList<string> PermissionCodes =
        [KonfeksiyonPermissions.Floor.Pause, KonfeksiyonPermissions.Floor.ReportBreakdown, KonfeksiyonPermissions.Floor.ResolveBreakdown, KonfeksiyonPermissions.Floor.ReportReject];

    public StaffRole Role { get; init; }

    public bool CanPause { get; init; }

    public bool CanReportBreakdown { get; init; }

    /// <summary>Arızanın giderildiğini kartını okutarak onaylayabilir mi.</summary>
    public bool CanResolveBreakdown { get; init; }

    /// <summary>Adet bildirirken hatalı adet girebilir mi (giremiyorsa yetkili kartıyla onaylanır).</summary>
    public bool CanReportReject { get; init; }

    public bool Allows(FloorAction action) => action switch
    {
        FloorAction.Pause => CanPause,
        FloorAction.ReportBreakdown => CanReportBreakdown,
        FloorAction.ResolveBreakdown => CanResolveBreakdown,
        FloorAction.ReportReject => CanReportReject,
        _ => false,
    };

    /// <summary>Görevin karşılık geldiği sistem rolü ("Roller ve Yetkiler" ekranındaki rol).</summary>
    public static string RoleCodeOf(StaffRole role) => role switch
    {
        StaffRole.LineChief => Roles.LineManager,
        StaffRole.Maintenance => Roles.Maintenance,
        _ => Roles.Operator,
    };

    /// <summary>Role verilmiş yetki kodlarından görevin tablet yetkilerini çıkarır.</summary>
    public static StaffRoleSetting FromPermissions(StaffRole role, IEnumerable<string> grantedCodes)
    {
        var granted = grantedCodes.ToHashSet();
        return new StaffRoleSetting
        {
            Role = role,
            CanPause = granted.Contains(KonfeksiyonPermissions.Floor.Pause),
            CanReportBreakdown = granted.Contains(KonfeksiyonPermissions.Floor.ReportBreakdown),
            CanResolveBreakdown = granted.Contains(KonfeksiyonPermissions.Floor.ResolveBreakdown),
            CanReportReject = granted.Contains(KonfeksiyonPermissions.Floor.ReportReject),
        };
    }

    /// <summary>İlk kurulumdaki hazır yetkiler: bant şefi hepsini yapar; bakım arızayı bildirir ve kapatır.</summary>
    public static StaffRoleSetting Default(StaffRole role) =>
        FromPermissions(role, KonfeksiyonRoles.DefaultPermissions[RoleCodeOf(role)]);
}

/// <summary>
/// Operatörün vesikalık fotoğrafı (tablette oturum açan personelin yanında gösterilir). Operatör
/// sorguları görüntü verisini taşımasın diye ayrı tabloda durur; operatör başına en fazla bir kayıt vardır.
/// </summary>
[NotAudited]
public class OperatorPhoto
{
    public int OperatorId { get; set; }
    public Operator Operator { get; set; } = null!;

    public byte[] Content { get; set; } = null!;

    /// <summary>image/jpeg ya da image/png.</summary>
    public string ContentType { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }
}
