using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Boyahane.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Boyahane.Application.Features.Floor;

public sealed record BoyahaneOperatorPhotoDto(string ContentType, string DataBase64, DateTime UpdatedAt);

/// <param name="ContentType">image/jpeg ya da image/png.</param>
/// <param name="DataBase64">Görüntünün base64 kodlanmış içeriği (en fazla 256 KB).</param>
public sealed record BoyahaneOperatorPhotoRequest(string ContentType, string DataBase64);

public sealed class BoyahaneOperatorPhotoRequestValidator : AbstractValidator<BoyahaneOperatorPhotoRequest>
{
    public const int MaxBytes = 256 * 1024;

    // Base64, veriyi 4/3 oranında büyütür; çözmeden önce kaba boyut denetimi için.
    private const int MaxBase64Length = (MaxBytes + 2) / 3 * 4;

    public BoyahaneOperatorPhotoRequestValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(type => BoyahaneOperatorCardService.Signatures.ContainsKey(type ?? string.Empty))
            .WithMessage("Fotoğraf JPEG ya da PNG olmalıdır.");
        RuleFor(x => x.DataBase64)
            .NotEmpty().WithMessage("Fotoğraf zorunludur.")
            .MaximumLength(MaxBase64Length).WithMessage($"Fotoğraf en fazla {MaxBytes / 1024} KB olabilir.");
    }
}

/// <param name="Machines">Boşsa makine kısıtı yoktur.</param>
public sealed record BoyahaneOperatorQualificationsDto(
    int OperatorId, string OperatorName, IReadOnlyList<LookupDto> Operations, IReadOnlyList<LookupDto> Machines);

public sealed record BoyahaneOperatorQualificationsRequest(IReadOnlyList<int> OperationIds, IReadOnlyList<int> MachineIds);

public interface IBoyahaneOperatorCardService
{
    Task<Result<BoyahaneOperatorPhotoDto>> GetPhotoAsync(int operatorId, CancellationToken cancellationToken);

    /// <summary>Operatörün fotoğrafını yükler; varsa eskisinin yerine geçer.</summary>
    Task<Result> SetPhotoAsync(int operatorId, BoyahaneOperatorPhotoRequest request, CancellationToken cancellationToken);

    Task<Result> RemovePhotoAsync(int operatorId, CancellationToken cancellationToken);

    /// <summary>Tablette oturumu açık operatörün fotoğrafı; oturum ya da fotoğraf yoksa boş.</summary>
    Task<Result<BoyahaneOperatorPhotoDto?>> GetSessionPhotoAsync(CancellationToken cancellationToken);

    Task<Result<BoyahaneOperatorQualificationsDto>> GetQualificationsAsync(int operatorId, CancellationToken cancellationToken);

    /// <summary>Operatörün yetkinliklerini verilen listelerle değiştirir.</summary>
    Task<Result<BoyahaneOperatorQualificationsDto>> ReplaceQualificationsAsync(int operatorId, BoyahaneOperatorQualificationsRequest request, CancellationToken cancellationToken);
}

public sealed class BoyahaneOperatorCardService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IBoyahaneOperatorCardService
{
    /// <summary>Kabul edilen türler ve dosyanın başında beklenen imza baytları.</summary>
    public static readonly IReadOnlyDictionary<string, byte[]> Signatures = new Dictionary<string, byte[]>
    {
        ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
        ["image/png"] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
    };

    private static readonly Error OperatorNotFound = new(ErrorCodes.NotFound, "Operatör bulunamadı.", ErrorType.NotFound);
    private static readonly Error PhotoNotFound = new(ErrorCodes.NotFound, "Operatörün fotoğrafı yok.", ErrorType.NotFound);

    public async Task<Result<BoyahaneOperatorPhotoDto>> GetPhotoAsync(int operatorId, CancellationToken cancellationToken)
    {
        if (!await OperatorExistsAsync(operatorId, cancellationToken))
        {
            return OperatorNotFound;
        }

        var photo = await FindPhotoAsync(operatorId, cancellationToken);
        return photo is null ? PhotoNotFound : photo;
    }

    public async Task<Result> SetPhotoAsync(int operatorId, BoyahaneOperatorPhotoRequest request, CancellationToken cancellationToken)
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

        var photo = await db.Set<BoyahaneOperatorPhoto>().SingleOrDefaultAsync(p => p.OperatorId == operatorId, cancellationToken);
        if (photo is null)
        {
            photo = new BoyahaneOperatorPhoto { OperatorId = operatorId };
            db.Set<BoyahaneOperatorPhoto>().Add(photo);
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

        await db.Set<BoyahaneOperatorPhoto>().Where(p => p.OperatorId == operatorId).ExecuteDeleteAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<BoyahaneOperatorPhotoDto?>> GetSessionPhotoAsync(CancellationToken cancellationToken)
    {
        var operatorId = await db.Set<BoyahaneTabletSession>().AsNoTracking()
            .Where(s => s.TabletId == currentUser.TabletId && s.EndedAt == null)
            .Select(s => (int?)s.OperatorId)
            .FirstOrDefaultAsync(cancellationToken);
        return operatorId is { } id ? await FindPhotoAsync(id, cancellationToken) : null;
    }

    public async Task<Result<BoyahaneOperatorQualificationsDto>> GetQualificationsAsync(int operatorId, CancellationToken cancellationToken)
    {
        var name = await db.Set<BoyahaneOperator>().Where(o => o.Id == operatorId).Select(o => o.Name).SingleOrDefaultAsync(cancellationToken);
        if (name is null)
        {
            return OperatorNotFound;
        }

        var operations = await db.Set<BoyahaneOperatorOperation>().AsNoTracking()
            .Where(q => q.OperatorId == operatorId)
            .OrderBy(q => q.Operation.Code)
            .Select(q => new LookupDto(q.OperationId, q.Operation.Code, q.Operation.Name))
            .ToListAsync(cancellationToken);
        var machines = await db.Set<BoyahaneOperatorMachine>().AsNoTracking()
            .Where(m => m.OperatorId == operatorId)
            .OrderBy(m => m.Machine.Code)
            .Select(m => new LookupDto(m.MachineId, m.Machine.Code, m.Machine.Name))
            .ToListAsync(cancellationToken);
        return new BoyahaneOperatorQualificationsDto(operatorId, name, operations, machines);
    }

    public async Task<Result<BoyahaneOperatorQualificationsDto>> ReplaceQualificationsAsync(int operatorId, BoyahaneOperatorQualificationsRequest request, CancellationToken cancellationToken)
    {
        if (!await OperatorExistsAsync(operatorId, cancellationToken))
        {
            return OperatorNotFound;
        }

        var operationIds = request.OperationIds.Distinct().ToList();
        var machineIds = request.MachineIds.Distinct().ToList();
        if (await db.Set<BoyahaneOperation>().CountAsync(o => operationIds.Contains(o.Id), cancellationToken) != operationIds.Count)
        {
            return ReferenceValidation.InvalidReference("operationIds", "Seçilen operasyonlardan biri bulunamadı.");
        }

        if (await db.Set<BoyahaneMachine>().CountAsync(m => machineIds.Contains(m.Id), cancellationToken) != machineIds.Count)
        {
            return ReferenceValidation.InvalidReference("machineIds", "Seçilen makinelerden biri bulunamadı.");
        }

        var currentOperations = await db.Set<BoyahaneOperatorOperation>().Where(q => q.OperatorId == operatorId).ToListAsync(cancellationToken);
        var currentMachines = await db.Set<BoyahaneOperatorMachine>().Where(m => m.OperatorId == operatorId).ToListAsync(cancellationToken);

        db.Set<BoyahaneOperatorOperation>().RemoveRange(currentOperations.Where(q => !operationIds.Contains(q.OperationId)));
        db.Set<BoyahaneOperatorOperation>().AddRange(operationIds
            .Except(currentOperations.Select(q => q.OperationId))
            .Select(id => new BoyahaneOperatorOperation { OperatorId = operatorId, OperationId = id }));

        db.Set<BoyahaneOperatorMachine>().RemoveRange(currentMachines.Where(m => !machineIds.Contains(m.MachineId)));
        db.Set<BoyahaneOperatorMachine>().AddRange(machineIds
            .Except(currentMachines.Select(m => m.MachineId))
            .Select(id => new BoyahaneOperatorMachine { OperatorId = operatorId, MachineId = id }));

        await db.SaveChangesAsync(cancellationToken);
        return await GetQualificationsAsync(operatorId, cancellationToken);
    }

    /// <summary>
    /// Konfeksiyon yetkinlik kuralı: makine listesi doluysa operatör yalnızca o makinelerde, operasyon verildiyse yalnızca
    /// yetkin olduğu operasyonda çalışır.
    /// </summary>
    public static async Task<Error?> CheckQualificationAsync(IAppDbContext db, int operatorId, int? machineId, int? operationId, CancellationToken cancellationToken)
    {
        if (machineId is { } machine)
        {
            var machineIds = await db.Set<BoyahaneOperatorMachine>()
                .Where(m => m.OperatorId == operatorId)
                .Select(m => m.MachineId)
                .ToListAsync(cancellationToken);
            if (machineIds.Count > 0 && !machineIds.Contains(machine))
            {
                return new Error(ErrorCodes.OperatorNotQualified, "Operatör bu makinede çalışmaya yetkili değil.");
            }
        }

        if (operationId is null
            || await db.Set<BoyahaneOperatorOperation>().AnyAsync(q => q.OperatorId == operatorId && q.OperationId == operationId, cancellationToken))
        {
            return null;
        }

        var operationName = await db.Set<BoyahaneOperation>().IgnoreQueryFilters().Where(o => o.Id == operationId).Select(o => o.Name).SingleOrDefaultAsync(cancellationToken);
        return new Error(ErrorCodes.OperatorNotQualified, $"Operatörün '{operationName}' operasyonu için yetkinliği yok.");
    }

    private Task<bool> OperatorExistsAsync(int operatorId, CancellationToken cancellationToken) =>
        db.Set<BoyahaneOperator>().AnyAsync(o => o.Id == operatorId, cancellationToken);

    private async Task<BoyahaneOperatorPhotoDto?> FindPhotoAsync(int operatorId, CancellationToken cancellationToken)
    {
        var photo = await db.Set<BoyahaneOperatorPhoto>().AsNoTracking().SingleOrDefaultAsync(p => p.OperatorId == operatorId, cancellationToken);
        return photo is null ? null : new BoyahaneOperatorPhotoDto(photo.ContentType, Convert.ToBase64String(photo.Content), photo.UpdatedAt);
    }

    /// <summary>Base64 çözülemiyorsa, boyut aşılıyorsa ya da dosya imzası türüyle uyuşmuyorsa boş döner.</summary>
    private static byte[]? Decode(BoyahaneOperatorPhotoRequest request)
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

        return content.Length <= BoyahaneOperatorPhotoRequestValidator.MaxBytes
            && Signatures.TryGetValue(request.ContentType, out var signature)
            && content.AsSpan().StartsWith(signature)
            ? content
            : null;
    }
}
