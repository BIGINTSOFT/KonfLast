using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Production;
using ERP.Konfeksiyon.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.WorkSessions;

/// <param name="ContentType">image/jpeg ya da image/png.</param>
/// <param name="DataBase64">Görüntünün base64 kodlanmış içeriği.</param>
public sealed record OperatorPhotoDto(string ContentType, string DataBase64, DateTime UpdatedAt);

/// <param name="ContentType">image/jpeg ya da image/png.</param>
/// <param name="DataBase64">Görüntünün base64 kodlanmış içeriği (en fazla 256 KB).</param>
public sealed record OperatorPhotoRequest(string ContentType, string DataBase64);

public sealed class OperatorPhotoRequestValidator : AbstractValidator<OperatorPhotoRequest>
{
    public const int MaxBytes = 256 * 1024;

    // Base64, veriyi 4/3 oranında büyütür; çözmeden önce kaba boyut denetimi için.
    private const int MaxBase64Length = (MaxBytes + 2) / 3 * 4;

    public OperatorPhotoRequestValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(type => OperatorPhotoService.Signatures.ContainsKey(type ?? string.Empty))
            .WithMessage("Fotoğraf JPEG ya da PNG olmalıdır.");
        RuleFor(x => x.DataBase64)
            .NotEmpty().WithMessage("Fotoğraf zorunludur.")
            .MaximumLength(MaxBase64Length).WithMessage($"Fotoğraf en fazla {MaxBytes / 1024} KB olabilir.");
    }
}

public interface IOperatorPhotoService
{
    Task<Result<OperatorPhotoDto>> GetAsync(int operatorId, CancellationToken cancellationToken);

    /// <summary>Operatörün fotoğrafını yükler; varsa eskisinin yerine geçer.</summary>
    Task<Result> SetAsync(int operatorId, OperatorPhotoRequest request, CancellationToken cancellationToken);

    Task<Result> RemoveAsync(int operatorId, CancellationToken cancellationToken);

    /// <summary>Tabletin makinesinde oturumu açık olan operatörün fotoğrafı; oturum ya da fotoğraf yoksa boş.</summary>
    Task<Result<OperatorPhotoDto?>> GetForStationAsync(CancellationToken cancellationToken);
}

public sealed class OperatorPhotoService(IKonfeksiyonDbContext db, IStationResolver stations, TimeProvider timeProvider)
    : IOperatorPhotoService
{
    /// <summary>Kabul edilen türler ve dosyanın başında beklenen imza baytları.</summary>
    public static readonly IReadOnlyDictionary<string, byte[]> Signatures = new Dictionary<string, byte[]>
    {
        ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
        ["image/png"] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
    };

    private static readonly Error OperatorNotFound = new(ErrorCodes.NotFound, "Operatör bulunamadı.", ErrorType.NotFound);
    private static readonly Error PhotoNotFound = new(ErrorCodes.NotFound, "Operatörün fotoğrafı yok.", ErrorType.NotFound);

    public async Task<Result<OperatorPhotoDto>> GetAsync(int operatorId, CancellationToken cancellationToken)
    {
        if (!await db.Operators.AnyAsync(o => o.Id == operatorId, cancellationToken))
        {
            return OperatorNotFound;
        }

        var photo = await FindAsync(operatorId, cancellationToken);
        return photo is null ? PhotoNotFound : photo;
    }

    public async Task<Result> SetAsync(int operatorId, OperatorPhotoRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Operators.AnyAsync(o => o.Id == operatorId, cancellationToken))
        {
            return OperatorNotFound;
        }

        var content = Decode(request);
        if (content is null)
        {
            return ReferenceValidation.InvalidReference("dataBase64", "Dosya geçerli bir JPEG ya da PNG görüntüsü değil.");
        }

        var photo = await db.OperatorPhotos.SingleOrDefaultAsync(p => p.OperatorId == operatorId, cancellationToken);
        if (photo is null)
        {
            photo = new OperatorPhoto { OperatorId = operatorId };
            db.OperatorPhotos.Add(photo);
        }

        photo.Content = content;
        photo.ContentType = request.ContentType;
        photo.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RemoveAsync(int operatorId, CancellationToken cancellationToken)
    {
        if (!await db.Operators.AnyAsync(o => o.Id == operatorId, cancellationToken))
        {
            return OperatorNotFound;
        }

        await db.OperatorPhotos.Where(p => p.OperatorId == operatorId).ExecuteDeleteAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<OperatorPhotoDto?>> GetForStationAsync(CancellationToken cancellationToken)
    {
        var resolved = await stations.ResolveAsync(cancellationToken);
        if (!resolved.IsSuccess)
        {
            return resolved.Error!;
        }

        var machineId = resolved.Value.MachineId;
        var operatorId = await db.WorkSessions
            .Where(s => s.MachineId == machineId && s.LogoutAt == null)
            .Select(s => (int?)s.OperatorId)
            .SingleOrDefaultAsync(cancellationToken);

        return operatorId is null ? null : await FindAsync(operatorId.Value, cancellationToken);
    }

    private async Task<OperatorPhotoDto?> FindAsync(int operatorId, CancellationToken cancellationToken)
    {
        var photo = await db.OperatorPhotos
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.OperatorId == operatorId, cancellationToken);
        return photo is null
            ? null
            : new OperatorPhotoDto(photo.ContentType, Convert.ToBase64String(photo.Content), photo.UpdatedAt);
    }

    /// <summary>Base64'ü çözer; boyut sınırını aşan ya da imzası türüyle uyuşmayan içerik için null döner.</summary>
    private static byte[]? Decode(OperatorPhotoRequest request)
    {
        var buffer = new byte[OperatorPhotoRequestValidator.MaxBytes];
        if (!Convert.TryFromBase64String(request.DataBase64, buffer, out var length))
        {
            return null;
        }

        var content = buffer.AsSpan(0, length);
        return content.StartsWith(Signatures[request.ContentType]) ? content.ToArray() : null;
    }
}
