using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Core.Application.Features.MasterData;

/// <param name="IsLocal">Yerel para birimi (TRY): kuru tutulmaz, silinemez, pasif yapılamaz.</param>
public sealed record CurrencyDto(
    int Id, string Code, string Name, string? Symbol, int DecimalPlaces, bool FetchFromTcmb, bool IsActive, bool IsLocal);

public sealed record CurrencyRequest(
    string Code, string Name, string? Symbol = null, int DecimalPlaces = 2, bool FetchFromTcmb = true, bool IsActive = true);

public sealed class CurrencyRequestValidator : AbstractValidator<CurrencyRequest>
{
    public CurrencyRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Kod zorunludur.")
            .Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Kod üç harfli ISO 4217 kodu olmalıdır (örn. USD).");
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.Symbol).OptionalText(5);
        RuleFor(x => x.DecimalPlaces).InclusiveBetween(0, 4).WithMessage("Ondalık hane 0 ile 4 arasında olmalıdır.");
    }
}

public sealed class CurrencyService(IAppDbContext db, IReferenceUsage usage) : MasterDataService<Currency, CurrencyDto, CurrencyRequest>(db)
{
    protected override DbSet<Currency> Set => Db.Currencies;
    protected override string Label => "Para birimi";
    protected override string CodeField => "code";

    protected override string CodeOf(CurrencyRequest request) => request.Code;
    protected override Expression<Func<Currency, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<Currency, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || e.Name.Contains(term);

    // Yerel para birimi her zaman başta.
    protected override IQueryable<Currency> Order(IQueryable<Currency> query) =>
        query.OrderByDescending(e => e.Code == Currency.LocalCode).ThenBy(e => e.Code);

    protected override Expression<Func<Currency, CurrencyDto>> ToDto => e => new CurrencyDto(
        e.Id, e.Code, e.Name, e.Symbol, e.DecimalPlaces, e.FetchFromTcmb, e.IsActive, e.Code == Currency.LocalCode);

    protected override Expression<Func<Currency, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(CurrencyRequest request, Currency entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Symbol = NullIfBlank(request.Symbol);
        entity.DecimalPlaces = request.DecimalPlaces;
        entity.FetchFromTcmb = code != Currency.LocalCode && request.FetchFromTcmb;
        entity.IsActive = request.IsActive;
    }

    protected override Task<Error?> ValidateAsync(CurrencyRequest request, Currency? existing, CancellationToken cancellationToken)
    {
        // Para birimine belgelerde koduyla bağlanılır; kod değişirse eski belgeler başka bir dövize bağlanmış olur.
        if (existing is not null && existing.Code != NormalizeCode(request.Code))
        {
            return Task.FromResult<Error?>(ReferenceValidation.InvalidReference(
                "code", "Kayıtlı para biriminin kodu değiştirilemez; yeni bir para birimi tanımlayın."));
        }

        if ((existing?.IsLocal ?? NormalizeCode(request.Code) == Currency.LocalCode) && !request.IsActive)
        {
            return Task.FromResult<Error?>(ReferenceValidation.InvalidReference("isActive", "Yerel para birimi pasif yapılamaz."));
        }

        return Task.FromResult<Error?>(null);
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        var code = await Db.Currencies.Where(c => c.Id == id).Select(c => c.Code).SingleAsync(cancellationToken);
        if (code == Currency.LocalCode)
        {
            return "yerel para birimi";
        }

        if (await Db.ExchangeRates.AnyAsync(r => r.CurrencyId == id, cancellationToken))
        {
            return "döviz kurları";
        }

        return await usage.FindAsync<Currency>(code, cancellationToken);
    }
}

/// <summary>Belgelerdeki para birimi kodlarının tanımlı para birimlerinden seçildiğini denetler.</summary>
public static class CurrencyRules
{
    /// <summary>
    /// Kod tanımlı ve aktif bir para birimi olmalıdır. Kayıtta zaten seçili olan (<paramref name="currentCode"/>)
    /// pasif para birimi korunabilir.
    /// </summary>
    public static async Task<Error?> RequireActiveAsync(
        IAppDbContext db, string? code, string? currentCode, string field, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized == currentCode)
        {
            // Kayıtta zaten seçili olan kod korunur (para birimi sonradan pasif yapılmış ya da tanımdan önce girilmiş olabilir).
            return null;
        }

        var currency = await db.Currencies.AsNoTracking()
            .Where(c => c.Code == normalized)
            .Select(c => new { c.IsActive })
            .SingleOrDefaultAsync(cancellationToken);
        if (currency is null)
        {
            return ReferenceValidation.InvalidReference(field, $"{normalized} tanımlı bir para birimi değil.");
        }

        return currency.IsActive
            ? null
            : ReferenceValidation.InvalidReference(field, $"{normalized} para birimi pasif.");
    }
}
