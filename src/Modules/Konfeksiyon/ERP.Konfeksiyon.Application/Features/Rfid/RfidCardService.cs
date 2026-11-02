using ERP.Konfeksiyon.Application.Abstractions;
using System.Linq.Expressions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.MasterData;
using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Rfid;

/// <summary>RFID kart tanımları (ana veri akışı). Kartın "kodu" normalize edilmiş UID'sidir.</summary>
public sealed class RfidCardService(IKonfeksiyonDbContext db) : KonfeksiyonMasterDataService<RfidCard, RfidCardDto, RfidCardRequest>(db)
{
    protected override DbSet<RfidCard> Set => Db.RfidCards;
    protected override string Label => "RFID kartı";
    protected override string CodeField => "uid";

    // Geçersiz UID doğrulamada reddedilir; buraya gelen değer normalize edilebilir.
    protected override string CodeOf(RfidCardRequest request) => RfidUid.Normalize(request.Uid) ?? request.Uid;
    protected override Expression<Func<RfidCard, bool>> WithCode(string code) => e => e.Uid == code;

    protected override Expression<Func<RfidCard, bool>> Matches(string term) =>
        e => e.Uid.Contains(term) || e.Label!.Contains(term);

    protected override IQueryable<RfidCard> Order(IQueryable<RfidCard> query) =>
        query.OrderBy(e => e.CardType).ThenBy(e => e.Label).ThenBy(e => e.Uid);

    protected override Expression<Func<RfidCard, RfidCardDto>> ToDto => e => new RfidCardDto(
        e.Id,
        e.Uid,
        e.CardType,
        e.Label,
        e.CardType == RfidCardType.Bundle
            ? Db.BundleCardAssignments
                .Where(a => a.RfidCardId == e.Id && a.ReleasedAt == null)
                .Select(a => "Demet " + a.Bundle.BundleNo)
                .FirstOrDefault()
            : Db.OperatorCards
                .Where(c => c.RfidCardId == e.Id && c.RevokedAt == null)
                .Select(c => c.Operator.FirstName + " " + c.Operator.LastName + " (" + c.Operator.OperatorCode + ")")
                .FirstOrDefault(),
        e.IsActive);

    protected override Expression<Func<RfidCard, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.Uid, e.Label ?? e.Uid);

    protected override void Apply(RfidCardRequest request, RfidCard entity, string code)
    {
        entity.Uid = code;
        entity.CardType = request.CardType;
        entity.Label = MasterDataRules.NullIfBlank(request.Label);
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(
        RfidCardRequest request, RfidCard? existing, CancellationToken cancellationToken)
    {
        if (existing is null || existing.CardType == request.CardType || !await IsAssignedAsync(existing.Id, cancellationToken))
        {
            return null;
        }

        const string message = "Atanmış bir kartın türü değiştirilemez; önce kartı bırakın.";
        return new Error(
            ErrorCodes.RfidCardAlreadyAssigned,
            message,
            ErrorType.Conflict,
            new Dictionary<string, string[]> { ["cardType"] = [message] });
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await IsAssignedAsync(id, cancellationToken) ? "kart şu an bir demete ya da operatöre atanmış" : null;

    private async Task<bool> IsAssignedAsync(int cardId, CancellationToken cancellationToken) =>
        await Db.BundleCardAssignments.AnyAsync(a => a.RfidCardId == cardId && a.ReleasedAt == null, cancellationToken)
        || await Db.OperatorCards.AnyAsync(c => c.RfidCardId == cardId && c.RevokedAt == null, cancellationToken);
}
