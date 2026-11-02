using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Dokuma.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Dokuma.Application.Features.Floor;

public sealed record DokumaOperatorPhotoDto(string ContentType, string DataBase64, DateTime UpdatedAt);

/// <param name="ContentType">image/jpeg ya da image/png.</param>
/// <param name="DataBase64">Görüntünün base64 kodlanmış içeriği (en fazla 256 KB).</param>
public sealed record DokumaOperatorPhotoRequest(string ContentType, string DataBase64);

public sealed class DokumaOperatorPhotoRequestValidator : AbstractValidator<DokumaOperatorPhotoRequest>
{
    public const int MaxBytes = 256 * 1024;

    // Base64, veriyi 4/3 oranında büyütür; çözmeden önce kaba boyut denetimi için.
    private const int MaxBase64Length = (MaxBytes + 2) / 3 * 4;

    public DokumaOperatorPhotoRequestValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(type => DokumaOperatorCardService.Signatures.ContainsKey(type ?? string.Empty))
            .WithMessage("Fotoğraf JPEG ya da PNG olmalıdır.");
        RuleFor(x => x.DataBase64)
            .NotEmpty().WithMessage("Fotoğraf zorunludur.")
            .MaximumLength(MaxBase64Length).WithMessage($"Fotoğraf en fazla {MaxBytes / 1024} KB olabilir.");
    }
}

/// <param name="Looms">Boşsa tezgâh kısıtı yoktur.</param>
public sealed record DokumaOperatorLoomsDto(int OperatorId, string OperatorName, IReadOnlyList<LookupDto> Looms);

public sealed record DokumaOperatorLoomsRequest(IReadOnlyList<int> LoomIds);

public interface IDokumaOperatorCardService
{
    Task<Result<DokumaOperatorPhotoDto>> GetPhotoAsync(int operatorId, CancellationToken cancellationToken);

    /// <summary>Operatörün fotoğrafını yükler; varsa eskisinin yerine geçer.</summary>
    Task<Result> SetPhotoAsync(int operatorId, DokumaOperatorPhotoRequest request, CancellationToken cancellationToken);

    Task<Result> RemovePhotoAsync(int operatorId, CancellationToken cancellationToken);

    /// <summary>Tablette oturumu açık operatörün fotoğrafı; oturum ya da fotoğraf yoksa boş.</summary>
    Task<Result<DokumaOperatorPhotoDto?>> GetSessionPhotoAsync(CancellationToken cancellationToken);

    Task<Result<DokumaOperatorLoomsDto>> GetLoomsAsync(int operatorId, CancellationToken cancellationToken);

    /// <summary>Operatörün çalışabileceği tezgâhları verilen listeyle değiştirir.</summary>
    Task<Result<DokumaOperatorLoomsDto>> ReplaceLoomsAsync(int operatorId, DokumaOperatorLoomsRequest request, CancellationToken cancellationToken);
}

public sealed class DokumaOperatorCardService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IDokumaOperatorCardService
{
    /// <summary>Kabul edilen türler ve dosyanın başında beklenen imza baytları.</summary>
    public static readonly IReadOnlyDictionary<string, byte[]> Signatures = new Dictionary<string, byte[]>
    {
        ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
        ["image/png"] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
    };

    private static readonly Error OperatorNotFound = new(ErrorCodes.NotFound, "Operatör bulunamadı.", ErrorType.NotFound);
    private static readonly Error PhotoNotFound = new(ErrorCodes.NotFound, "Operatörün fotoğrafı yok.", ErrorType.NotFound);

    public async Task<Result<DokumaOperatorPhotoDto>> GetPhotoAsync(int operatorId, CancellationToken cancellationToken)
    {
        if (!await OperatorExistsAsync(operatorId, cancellationToken))
        {
            return OperatorNotFound;
        }

        var photo = await FindPhotoAsync(operatorId, cancellationToken);
        return photo is null ? PhotoNotFound : photo;
    }

    public async Task<Result> SetPhotoAsync(int operatorId, DokumaOperatorPhotoRequest request, CancellationToken cancellationToken)
    {
        if (!await OperatorExistsAsync(operatorId, cancellationToken))
        {
            return OperatorNotFound;
        }

        var content = Decode(request);
        if (content is null)
        {
            return ReferenceValidation.InvalidReference("dataBase64", "Dosya geçerli bir JPEG ya da PNG görüntüsü değil.");
        }

        var photo = await db.Set<DokumaOperatorPhoto>().SingleOrDefaultAsync(p => p.OperatorId == operatorId, cancellationToken);
        if (photo is null)
        {
            photo = new DokumaOperatorPhoto { OperatorId = operatorId };
            db.Set<DokumaOperatorPhoto>().Add(photo);
        }

        photo.Content = content;
        photo.ContentType = request.ContentType;
        photo.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RemovePhotoAsync(int operatorId, CancellationToken cancellationToken)
    {
        if (!await OperatorExistsAsync(operatorId, cancellationToken))
        {
            return OperatorNotFound;
        }

        await db.Set<DokumaOperatorPhoto>().Where(p => p.OperatorId == operatorId).ExecuteDeleteAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<DokumaOperatorPhotoDto?>> GetSessionPhotoAsync(CancellationToken cancellationToken)
    {
        var operatorId = await db.Set<DokumaTabletSession>().AsNoTracking()
            .Where(s => s.TabletId == currentUser.TabletId && s.EndedAt == null)
            .Select(s => (int?)s.OperatorId)
            .FirstOrDefaultAsync(cancellationToken);
        return operatorId is { } id ? await FindPhotoAsync(id, cancellationToken) : null;
    }

    public async Task<Result<DokumaOperatorLoomsDto>> GetLoomsAsync(int operatorId, CancellationToken cancellationToken)
    {
        var name = await db.Set<DokumaOperator>().Where(o => o.Id == operatorId).Select(o => o.Name).SingleOrDefaultAsync(cancellationToken);
        if (name is null)
        {
            return OperatorNotFound;
        }

        var looms = await db.Set<DokumaOperatorLoom>().AsNoTracking()
            .Where(l => l.OperatorId == operatorId)
            .OrderBy(l => l.Loom.Code)
            .Select(l => new LookupDto(l.LoomId, l.Loom.Code, l.Loom.Name))
            .ToListAsync(cancellationToken);
        return new DokumaOperatorLoomsDto(operatorId, name, looms);
    }

    public async Task<Result<DokumaOperatorLoomsDto>> ReplaceLoomsAsync(int operatorId, DokumaOperatorLoomsRequest request, CancellationToken cancellationToken)
    {
        if (!await OperatorExistsAsync(operatorId, cancellationToken))
        {
            return OperatorNotFound;
        }

        var loomIds = request.LoomIds.Distinct().ToList();
        if (await db.Set<DokumaLoom>().CountAsync(l => loomIds.Contains(l.Id), cancellationToken) != loomIds.Count)
        {
            return ReferenceValidation.InvalidReference("loomIds", "Seçilen tezgâhlardan biri bulunamadı.");
        }

        var current = await db.Set<DokumaOperatorLoom>().Where(l => l.OperatorId == operatorId).ToListAsync(cancellationToken);
        db.Set<DokumaOperatorLoom>().RemoveRange(current.Where(l => !loomIds.Contains(l.LoomId)));
        db.Set<DokumaOperatorLoom>().AddRange(loomIds
            .Except(current.Select(l => l.LoomId))
            .Select(id => new DokumaOperatorLoom { OperatorId = operatorId, LoomId = id }));

        await db.SaveChangesAsync(cancellationToken);
        return await GetLoomsAsync(operatorId, cancellationToken);
    }

    /// <summary>Konfeksiyon yetkinlik kuralı: tezgâh listesi doluysa operatör yalnızca o tezgâhlarda çalışır.</summary>
    public static async Task<Error?> CheckLoomAsync(IAppDbContext db, int operatorId, int loomId, CancellationToken cancellationToken)
    {
        var loomIds = await db.Set<DokumaOperatorLoom>().Where(l => l.OperatorId == operatorId).Select(l => l.LoomId).ToListAsync(cancellationToken);
        return loomIds.Count > 0 && !loomIds.Contains(loomId)
            ? new Error(ErrorCodes.OperatorNotQualified, "Operatör bu tezgâhta çalışmaya yetkili değil.")
            : null;
    }

    private Task<bool> OperatorExistsAsync(int operatorId, CancellationToken cancellationToken) =>
        db.Set<DokumaOperator>().AnyAsync(o => o.Id == operatorId, cancellationToken);

    private async Task<DokumaOperatorPhotoDto?> FindPhotoAsync(int operatorId, CancellationToken cancellationToken)
    {
        var photo = await db.Set<DokumaOperatorPhoto>().AsNoTracking().SingleOrDefaultAsync(p => p.OperatorId == operatorId, cancellationToken);
        return photo is null ? null : new DokumaOperatorPhotoDto(photo.ContentType, Convert.ToBase64String(photo.Content), photo.UpdatedAt);
    }

    /// <summary>Base64 çözülemiyorsa, boyut aşılıyorsa ya da dosya imzası türüyle uyuşmuyorsa boş döner.</summary>
    private static byte[]? Decode(DokumaOperatorPhotoRequest request)
    {
        byte[] content;
        try
        {
            content = Convert.FromBase64String(request.DataBase64);
        }
        catch (FormatException)
        {
            return null;
        }

        return content.Length <= DokumaOperatorPhotoRequestValidator.MaxBytes
            && Signatures.TryGetValue(request.ContentType, out var signature)
            && content.AsSpan().StartsWith(signature)
            ? content
            : null;
    }
}
