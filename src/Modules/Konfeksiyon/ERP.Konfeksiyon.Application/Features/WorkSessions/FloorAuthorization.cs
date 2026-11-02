using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.WorkSessions;

/// <summary>Bir saha işlemini yapan ya da kartıyla onaylayan personel.</summary>
/// <param name="ByCard">İşlem, bu amaçla okutulan yetkili kartıyla mı onaylandı (oturum sahibinin kendi yetkisiyle değil).</param>
public sealed record FloorActor(int OperatorId, string DisplayName, bool ByCard);

public interface IFloorAuthorization
{
    /// <summary>
    /// Tabletteki yetki gerektiren işlemi (ara, devam, arıza bildirme, arıza kapatma) kimin yaptığını belirler.
    /// Bu işlemler her zaman kart ister. Görevi yetkili olan kişi (bant şefi vb.) her tablette; kişiye özel yetkisi
    /// olan operatör yalnızca kendi oturumunun açık olduğu tablette onaylar. Yalnızca hatalı adet bildiriminde
    /// oturum sahibinin kendi yetkisi kartsız geçer. Yetki yoksa AUTHORIZATION_CARD_REQUIRED (kart okutulmalı)
    /// ya da NOT_AUTHORIZED_FOR_ACTION (okutulan kartın sahibi yetkili değil) döner.
    /// </summary>
    Task<Result<FloorActor>> AuthorizeAsync(
        FloorAction action, int? sessionOperatorId, string? authorizerUid, CancellationToken cancellationToken);
}

public sealed class FloorAuthorization(IKonfeksiyonDbContext db, IStaffRoleSettingService roleSettings) : IFloorAuthorization
{
    private static readonly IReadOnlyDictionary<StaffRole, string> RoleNames = new Dictionary<StaffRole, string>
    {
        [StaffRole.Operator] = "Operatör",
        [StaffRole.LineChief] = "Bant Şefi",
        [StaffRole.Maintenance] = "Bakım Operatörü",
    };

    public async Task<Result<FloorActor>> AuthorizeAsync(
        FloorAction action, int? sessionOperatorId, string? authorizerUid, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(authorizerUid))
        {
            var holder = await FindCardHolderAsync(authorizerUid, cancellationToken);
            if (!holder.IsSuccess)
            {
                return holder.Error!;
            }

            var setting = await roleSettings.GetAsync(holder.Value.Role, cancellationToken);
            if (setting.Allows(action))
            {
                return Actor(holder.Value, byCard: true);
            }

            if (holder.Value.Allows(action, setting))
            {
                // Kişiye özel yetki yalnızca kendi oturumunda geçer; başka tabletin işini operatör başlatıp bitiremez.
                return holder.Value.Id == sessionOperatorId
                    ? Actor(holder.Value, byCard: true)
                    : new Error(
                        ErrorCodes.NotAuthorizedForAction,
                        $"{NameOf(holder.Value)} bu işlemi yalnızca kendi oturumunun açık olduğu tablette yapabilir. {await WhoCanAsync(action, cancellationToken)}",
                        ErrorType.Forbidden);
            }

            return new Error(
                ErrorCodes.NotAuthorizedForAction,
                $"{NameOf(holder.Value)} bu işlem için yetkili değil. {await WhoCanAsync(action, cancellationToken)}",
                ErrorType.Forbidden);
        }

        if (action == FloorAction.ReportReject && sessionOperatorId is { } operatorId)
        {
            var sessionOperator = await db.Operators
                .AsNoTracking()
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(o => o.Id == operatorId, cancellationToken);
            if (sessionOperator is not null
                && sessionOperator.Allows(action, await roleSettings.GetAsync(sessionOperator.Role, cancellationToken)))
            {
                return Actor(sessionOperator, byCard: false);
            }
        }

        return new Error(
            ErrorCodes.AuthorizationCardRequired, await WhoCanAsync(action, cancellationToken), ErrorType.Forbidden);
    }

    /// <summary>Okutulan personel kartının sahibi: kart tanımlı, aktif ve aktif bir personele atanmış olmalı.</summary>
    private async Task<Result<Operator>> FindCardHolderAsync(string rawUid, CancellationToken cancellationToken)
    {
        var uid = RfidUid.Normalize(rawUid);
        if (uid is null)
        {
            return new Error(ErrorCodes.InvalidRfidUid, "Kart okunamadı. Lütfen kartı yeniden okutun.", ErrorType.Validation);
        }

        var card = await db.RfidCards
            .AsNoTracking()
            .Where(c => c.Uid == uid)
            .Select(c => new { c.Id, c.IsActive, c.CardType })
            .SingleOrDefaultAsync(cancellationToken);
        if (card is null)
        {
            return new Error(ErrorCodes.RfidNotRegistered, "Kart sistemde tanımlı değil.", ErrorType.NotFound);
        }

        if (!card.IsActive || card.CardType != RfidCardType.Operator)
        {
            return new Error(ErrorCodes.NotAuthorizedForAction, "Bu kart yetkili personel kartı değil.", ErrorType.Forbidden);
        }

        var holder = await db.OperatorCards
            .AsNoTracking()
            .Where(c => c.RfidCardId == card.Id && c.RevokedAt == null)
            .Select(c => c.Operator)
            .SingleOrDefaultAsync(cancellationToken);
        if (holder is null || !holder.IsActive)
        {
            return new Error(
                ErrorCodes.NotAuthorizedForAction, "Bu kart aktif bir personele atanmamış.", ErrorType.Forbidden);
        }

        return holder;
    }

    private static FloorActor Actor(Operator person, bool byCard) => new(person.Id, NameOf(person), byCard);

    private static string NameOf(Operator person) => $"{person.FirstName} {person.LastName} ({person.OperatorCode})";

    /// <summary>İşlemi hangi görevlerin kartıyla yapılabileceğini operatöre söyleyen yönlendirme.</summary>
    private async Task<string> WhoCanAsync(FloorAction action, CancellationToken cancellationToken)
    {
        var roles = (await roleSettings.GetAllAsync(cancellationToken))
            .Where(setting => setting.Allows(action))
            .Select(setting => RoleNames[setting.Role])
            .ToList();
        var who = roles.Count == 0 ? "Yetkili personel kartını okutun." : $"Yetkili kartını okutun: {string.Join(", ", roles)}.";

        return action switch
        {
            FloorAction.Pause => $"Ara vermek ve devam etmek için kart gerekli. {who}",
            FloorAction.ReportBreakdown => $"Arıza bildirmek için kart gerekli. {who}",
            FloorAction.ReportReject => $"Hatalı adet bildirmek için yetki gerekli. {who}",
            _ => $"Arızayı yalnızca yetkili personel kapatabilir. {who}",
        };
    }
}
