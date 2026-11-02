using ERP.Core.Application.Common;
using FluentValidation;

namespace ERP.Core.Application.Features.MasterData;

public sealed class MasterDataQuery : PageRequest
{
    /// <summary>Kod veya ad içinde aranır.</summary>
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class LookupQuery
{
    public const int DefaultTake = 50;
    public const int MaxTake = 100;

    public string? Search { get; set; }
    public int Take { get; set; } = DefaultTake;

    /// <summary>Düzenlenen kayıtta seçili olan (pasif olsa bile) değerin listede yer alması için.</summary>
    public int? IncludeId { get; set; }

    /// <summary>Müşteriye göre süzme destekleyen kaynaklarda (ör. ürünler) yalnızca bu müşteriye satılabilenler.</summary>
    public int? CustomerId { get; set; }

    public int NormalizedTake => Take < 1 ? DefaultTake : Math.Min(Take, MaxTake);
}

/// <summary>Seçim kutuları için hafif kayıt.</summary>
public sealed record LookupDto(int Id, string Code, string Name)
{
    /// <summary>Seçimde kodun ve adın yanında gösterilen ek bilgi (ör. modelin müşterisi).</summary>
    public string? Detail { get; init; }
}

public interface IMasterDataService<TDto, in TRequest>
{
    Task<PagedResult<TDto>> GetListAsync(MasterDataQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<LookupDto>> LookupAsync(LookupQuery query, CancellationToken cancellationToken);
    Task<Result<TDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<TDto>> CreateAsync(TRequest request, CancellationToken cancellationToken);
    Task<Result<TDto>> UpdateAsync(int id, TRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

public static class MasterDataRules
{
    public const int CodeMaxLength = 30;

    /// <summary>
    /// Kodlar etiket, barkod ve raporlarda kullanılır: yalnızca ASCII harf, rakam ve . _ - /
    /// Büyük/küçük harf ayrımı yoktur; büyük harfe çevrilerek saklanır.
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidCode<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("Kod zorunludur.")
        .Must(code => code is null || code.Trim().Length <= CodeMaxLength)
        .WithMessage($"Kod en fazla {CodeMaxLength} karakter olabilir.")
        // Baştaki / sondaki boşluklar kayıt sırasında kırpılır; aradaki boşluk geçersizdir.
        .Matches(@"^\s*[A-Za-z0-9._/-]+\s*$").WithMessage("Kod yalnızca harf (A-Z), rakam ve . _ - / içerebilir.");

    public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> rule, int maxLength) => rule
        .NotEmpty().WithMessage("Ad zorunludur.")
        .MaximumLength(maxLength).WithMessage($"Ad en fazla {maxLength} karakter olabilir.");

    public static IRuleBuilderOptions<T, string?> OptionalText<T>(
        this IRuleBuilder<T, string?> rule, int maxLength) => rule
        .MaximumLength(maxLength).WithMessage($"En fazla {maxLength} karakter olabilir.");

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    public static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
