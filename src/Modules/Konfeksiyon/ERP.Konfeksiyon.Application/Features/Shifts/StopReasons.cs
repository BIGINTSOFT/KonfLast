using ERP.Konfeksiyon.Application.Abstractions;
using System.Linq.Expressions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.MasterData;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Shifts;

/// <param name="Category">PAUSE (ara nedeni) ya da BREAKDOWN (arıza nedeni).</param>
/// <param name="SortOrder">Tabletteki sıra; küçük olan önce gösterilir.</param>
public sealed record StopReasonDto(int Id, string Code, string Name, StopReasonCategory Category, int SortOrder, bool IsActive);

public sealed record StopReasonRequest(
    string Code, string Name, StopReasonCategory Category = StopReasonCategory.Pause, int SortOrder = 0, bool IsActive = true);

/// <summary>Tabletin seçenek olarak gösterdiği duruş nedeni.</summary>
public sealed record StopReasonOptionDto(int Id, string Code, string Name);

public sealed class StopReasonRequestValidator : AbstractValidator<StopReasonRequest>
{
    public StopReasonRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ad zorunludur.")
            .MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir.");
        RuleFor(x => x.Category).IsInEnum().WithMessage("Geçersiz neden türü.");
        RuleFor(x => x.SortOrder).InclusiveBetween(0, 9999).WithMessage("Sıra 0 ile 9999 arasında olmalıdır.");
    }
}

/// <summary>Duruş nedenleri (ana veri akışı): ara ve arıza nedenleri.</summary>
public sealed class StopReasonService(IKonfeksiyonDbContext db) : KonfeksiyonMasterDataService<StopReason, StopReasonDto, StopReasonRequest>(db)
{
    protected override DbSet<StopReason> Set => Db.StopReasons;
    protected override string Label => "Duruş nedeni";
    protected override string CodeField => "code";

    protected override string CodeOf(StopReasonRequest request) => request.Code;
    protected override Expression<Func<StopReason, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<StopReason, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<StopReason> Order(IQueryable<StopReason> query) =>
        query.OrderBy(e => e.Category).ThenBy(e => e.SortOrder).ThenBy(e => e.Name);

    protected override Expression<Func<StopReason, StopReasonDto>> ToDto =>
        e => new StopReasonDto(e.Id, e.Code, e.Name, e.Category, e.SortOrder, e.IsActive);

    protected override Expression<Func<StopReason, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(StopReasonRequest request, StopReason entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Category = request.Category;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
    }

    // Kullanılmış neden silinse de geçmiş duraklama kayıtlarında adıyla görünmeye devam eder (soft delete).
}

public interface IStopReasonLookup
{
    /// <summary>Tabletin göstereceği aktif nedenler, sırasıyla.</summary>
    Task<IReadOnlyList<StopReasonOptionDto>> GetOptionsAsync(StopReasonCategory category, CancellationToken cancellationToken);

    /// <summary>Seçilen nedeni doğrular: var, aktif ve beklenen türde olmalı. Uygunsa nedeni, değilse hatayı döner.</summary>
    Task<Result<StopReasonOptionDto>> ResolveAsync(int id, StopReasonCategory category, CancellationToken cancellationToken);
}

public sealed class StopReasonLookup(IKonfeksiyonDbContext db) : IStopReasonLookup
{
    public async Task<IReadOnlyList<StopReasonOptionDto>> GetOptionsAsync(
        StopReasonCategory category, CancellationToken cancellationToken) =>
        await db.StopReasons
            .AsNoTracking()
            .Where(r => r.Category == category && r.IsActive)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .Select(r => new StopReasonOptionDto(r.Id, r.Code, r.Name))
            .ToListAsync(cancellationToken);

    public async Task<Result<StopReasonOptionDto>> ResolveAsync(
        int id, StopReasonCategory category, CancellationToken cancellationToken)
    {
        var reason = await db.StopReasons
            .AsNoTracking()
            .Where(r => r.Id == id && r.Category == category && r.IsActive)
            .Select(r => new StopReasonOptionDto(r.Id, r.Code, r.Name))
            .SingleOrDefaultAsync(cancellationToken);
        if (reason is null)
        {
            return ReferenceValidation.InvalidReference(
                "reasonId",
                category == StopReasonCategory.Pause ? "Geçerli bir ara nedeni seçin." : "Geçerli bir arıza nedeni seçin.");
        }

        return reason;
    }
}
