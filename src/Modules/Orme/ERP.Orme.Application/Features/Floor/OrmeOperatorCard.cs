using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Orme.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Orme.Application.Features.Floor;

public sealed record OrmeOperatorPhotoDto(string ContentType, string DataBase64, DateTime UpdatedAt);

/// <param name="ContentType">image/jpeg ya da image/png.</param>
/// <param name="DataBase64">Görüntünün base64 kodlanmış içeriği (en fazla 256 KB).</param>
public sealed record OrmeOperatorPhotoRequest(string ContentType, string DataBase64);

public sealed class OrmeOperatorPhotoRequestValidator : AbstractValidator<OrmeOperatorPhotoRequest>
{
    public const int MaxBytes = 256 * 1024;

    // Base64, veriyi 4/3 oranında büyütür; çözmeden önce kaba boyut denetimi için.
    private const int MaxBase64Length = (MaxBytes + 2) / 3 * 4;

    public OrmeOperatorPhotoRequestValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(type => OrmeOperatorCardService.Signatures.ContainsKey(type ?? string.Empty))
            .WithMessage("Fotoğraf JPEG ya da PNG olmalıdır.");
        RuleFor(x => x.DataBase64)
            .NotEmpty().WithMessage("Fotoğraf zorunludur.")
            .MaximumLength(MaxBase64Length).WithMessage($"Fotoğraf en fazla {MaxBytes / 1024} KB olabilir.");
    }
}

/// <param name="Machines">Boşsa makine kısıtı yoktur.</param>
public sealed record OrmeOperatorMachinesDto(int OperatorId, string OperatorName, IReadOnlyList<LookupDto> Machines);

public sealed record OrmeOperatorMachinesRequest(IReadOnlyList<int> MachineIds);

public interface IOrmeOperatorCardService
{
    Task<Result<OrmeOperatorPhotoDto>> GetPhotoAsync(int operatorId, CancellationToken cancellationToken);

    /// <summary>Operatörün fotoğrafını yükler; varsa eskisinin yerine geçer.</summary>
    Task<Result> SetPhotoAsync(int operatorId, OrmeOperatorPhotoRequest request, CancellationToken cancellationToken);

    Task<Result> RemovePhotoAsync(int operatorId, CancellationToken cancellationToken);

    /// <summary>Tablette oturumu açık operatörün fotoğrafı; oturum ya da fotoğraf yoksa boş.</summary>
    Task<Result<OrmeOperatorPhotoDto?>> GetSessionPhotoAsync(CancellationToken cancellationToken);

    Task<Result<OrmeOperatorMachinesDto>> GetMachinesAsync(int operatorId, CancellationToken cancellationToken);

    /// <summary>Operatörün çalışabileceği makineleri verilen listeyle değiştirir.</summary>
    Task<Result<OrmeOperatorMachinesDto>> ReplaceMachinesAsync(int operatorId, OrmeOperatorMachinesRequest request, CancellationToken cancellationToken);
}

public sealed class OrmeOperatorCardService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IOrmeOperatorCardService
{
    /// <summary>Kabul edilen türler ve dosyanın başında beklenen imza baytları.</summary>
    public static readonly IReadOnlyDictionary<string, byte[]> Signatures = new Dictionary<string, byte[]>
    {
        ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
        ["image/png"] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
    };

    private static readonly Error OperatorNotFound = new(ErrorCodes.NotFound, "Operatör bulunamadı.", ErrorType.NotFound);
    private static readonly Error PhotoNotFound = new(ErrorCodes.NotFound, "Operatörün fotoğrafı yok.", ErrorType.NotFound);

    public async Task<Result<OrmeOperatorPhotoDto>> GetPhotoAsync(int operatorId, CancellationToken cancellationToken)
    {
        if (!await OperatorExistsAsync(operatorId, cancellationToken))
        {
            return OperatorNotFound;
        }

        var photo = await FindPhotoAsync(operatorId, cancellationToken);
        return photo is null ? PhotoNotFound : photo;
    }

    public async Task<Result> SetPhotoAsync(int operatorId, OrmeOperatorPhotoRequest request, CancellationToken cancellationToken)
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

        var photo = await db.Set<OrmeOperatorPhoto>().SingleOrDefaultAsync(p => p.OperatorId == operatorId, cancellationToken);
        if (photo is null)
        {
            photo = new OrmeOperatorPhoto { OperatorId = operatorId };
            db.Set<OrmeOperatorPhoto>().Add(photo);
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

        await db.Set<OrmeOperatorPhoto>().Where(p => p.OperatorId == operatorId).ExecuteDeleteAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<OrmeOperatorPhotoDto?>> GetSessionPhotoAsync(CancellationToken cancellationToken)
    {
        var operatorId = await db.Set<OrmeTabletSession>().AsNoTracking()
            .Where(s => s.TabletId == currentUser.TabletId && s.EndedAt == null)
            .Select(s => (int?)s.OperatorId)
            .FirstOrDefaultAsync(cancellationToken);
        return operatorId is { } id ? await FindPhotoAsync(id, cancellationToken) : null;
    }

    public async Task<Result<OrmeOperatorMachinesDto>> GetMachinesAsync(int operatorId, CancellationToken cancellationToken)
    {
        var name = await db.Set<OrmeOperator>().Where(o => o.Id == operatorId).Select(o => o.Name).SingleOrDefaultAsync(cancellationToken);
        if (name is null)
        {
            return OperatorNotFound;
        }

        var machines = await db.Set<OrmeOperatorMachine>().AsNoTracking()
            .Where(l => l.OperatorId == operatorId)
            .OrderBy(l => l.Machine.Code)
            .Select(l => new LookupDto(l.MachineId, l.Machine.Code, l.Machine.Name))
            .ToListAsync(cancellationToken);
        return new OrmeOperatorMachinesDto(operatorId, name, machines);
    }

    public async Task<Result<OrmeOperatorMachinesDto>> ReplaceMachinesAsync(int operatorId, OrmeOperatorMachinesRequest request, CancellationToken cancellationToken)
    {
        if (!await OperatorExistsAsync(operatorId, cancellationToken))
        {
            return OperatorNotFound;
        }

        var machineIds = request.MachineIds.Distinct().ToList();
        if (await db.Set<OrmeMachine>().CountAsync(l => machineIds.Contains(l.Id), cancellationToken) != machineIds.Count)
        {
            return ReferenceValidation.InvalidReference("machineIds", "Seçilen makinelerden biri bulunamadı.");
        }

        var current = await db.Set<OrmeOperatorMachine>().Where(l => l.OperatorId == operatorId).ToListAsync(cancellationToken);
        db.Set<OrmeOperatorMachine>().RemoveRange(current.Where(l => !machineIds.Contains(l.MachineId)));
        db.Set<OrmeOperatorMachine>().AddRange(machineIds
            .Except(current.Select(l => l.MachineId))
            .Select(id => new OrmeOperatorMachine { OperatorId = operatorId, MachineId = id }));

        await db.SaveChangesAsync(cancellationToken);
        return await GetMachinesAsync(operatorId, cancellationToken);
    }

    /// <summary>Konfeksiyon yetkinlik kuralı: makine listesi doluysa operatör yalnızca o makinelerde çalışır.</summary>
    public static async Task<Error?> CheckMachineAsync(IAppDbContext db, int operatorId, int machineId, CancellationToken cancellationToken)
    {
        var machineIds = await db.Set<OrmeOperatorMachine>().Where(l => l.OperatorId == operatorId).Select(l => l.MachineId).ToListAsync(cancellationToken);
        return machineIds.Count > 0 && !machineIds.Contains(machineId)
            ? new Error(ErrorCodes.OperatorNotQualified, "Operatör bu makinede çalışmaya yetkili değil.")
            : null;
    }

    private Task<bool> OperatorExistsAsync(int operatorId, CancellationToken cancellationToken) =>
        db.Set<OrmeOperator>().AnyAsync(o => o.Id == operatorId, cancellationToken);

    private async Task<OrmeOperatorPhotoDto?> FindPhotoAsync(int operatorId, CancellationToken cancellationToken)
    {
        var photo = await db.Set<OrmeOperatorPhoto>().AsNoTracking().SingleOrDefaultAsync(p => p.OperatorId == operatorId, cancellationToken);
        return photo is null ? null : new OrmeOperatorPhotoDto(photo.ContentType, Convert.ToBase64String(photo.Content), photo.UpdatedAt);
    }

    /// <summary>Base64 çözülemiyorsa, boyut aşılıyorsa ya da dosya imzası türüyle uyuşmuyorsa boş döner.</summary>
    private static byte[]? Decode(OrmeOperatorPhotoRequest request)
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

        return content.Length <= OrmeOperatorPhotoRequestValidator.MaxBytes
            && Signatures.TryGetValue(request.ContentType, out var signature)
            && content.AsSpan().StartsWith(signature)
            ? content
            : null;
    }
}
