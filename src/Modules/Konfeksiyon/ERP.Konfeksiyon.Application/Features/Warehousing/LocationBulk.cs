using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Warehousing;

/// <summary>
/// Toplu adres açma: bir bölgede raf (ve isteğe bağlı göz) aralığı. Örn. bölge A, raf 1-10, göz 1-4, 2 basamak
/// → A-01-01 … A-10-04.
/// </summary>
/// <param name="BinFrom">Göz aralığı; verilmezse adresler raf düzeyindedir (A-01 … A-10).</param>
/// <param name="Digits">Raf ve göz numaralarının basamak sayısı (baştaki sıfırlarla tamamlanır).</param>
public sealed record BulkLocationRequest(
    int WarehouseId, string Zone, int RackFrom, int RackTo, int? BinFrom = null, int? BinTo = null, int Digits = 2);

/// <param name="Skipped">Depoda zaten var olduğu için açılmayan adres sayısı.</param>
public sealed record BulkLocationResultDto(int Created, int Skipped, string? FirstCode, string? LastCode);

public sealed class BulkLocationRequestValidator : AbstractValidator<BulkLocationRequest>
{
    public const int MaxLocations = 2000;
    public const int MaxNumber = 9999;

    public BulkLocationRequestValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("Depo seçilmelidir.");
        RuleFor(x => x.Zone)
            .NotEmpty().WithMessage("Bölge zorunludur.")
            .Must(zone => zone is null || (zone.Trim() is { Length: <= WarehouseLocationRequestValidator.SegmentMaxLength } segment
                && segment.All(char.IsAsciiLetterOrDigit)))
            .WithMessage($"Bölge yalnızca harf (A-Z) ve rakam içerebilir, en fazla {WarehouseLocationRequestValidator.SegmentMaxLength} karakter olabilir.");
        RuleFor(x => x.Digits).InclusiveBetween(1, 4).WithMessage("Basamak sayısı 1 ile 4 arasında olmalıdır.");
        RuleFor(x => x.RackFrom).InclusiveBetween(0, MaxNumber).WithMessage($"Raf numarası 0 ile {MaxNumber} arasında olmalıdır.");
        RuleFor(x => x.RackTo)
            .InclusiveBetween(0, MaxNumber).WithMessage($"Raf numarası 0 ile {MaxNumber} arasında olmalıdır.")
            .GreaterThanOrEqualTo(x => x.RackFrom).WithMessage("Raf aralığının sonu başından küçük olamaz.");
        RuleFor(x => x.BinFrom)
            .Must((request, from) => (from is null) == (request.BinTo is null))
            .WithMessage("Göz aralığının başı ve sonu birlikte verilmelidir.");
        When(x => x.BinFrom is not null && x.BinTo is not null, () =>
        {
            RuleFor(x => x.BinFrom!.Value).InclusiveBetween(0, MaxNumber).WithMessage($"Göz numarası 0 ile {MaxNumber} arasında olmalıdır.");
            RuleFor(x => x.BinTo!.Value)
                .InclusiveBetween(0, MaxNumber).WithMessage($"Göz numarası 0 ile {MaxNumber} arasında olmalıdır.")
                .GreaterThanOrEqualTo(x => x.BinFrom!.Value).WithMessage("Göz aralığının sonu başından küçük olamaz.");
        });
        RuleFor(x => x)
            .Must(x => Count(x) <= MaxLocations)
            .WithName("rackTo")
            .WithMessage($"Tek seferde en fazla {MaxLocations} adres açılabilir; aralığı daraltın.")
            .When(x => x.RackTo >= x.RackFrom && (x.BinFrom is null || x.BinTo >= x.BinFrom));
    }

    private static long Count(BulkLocationRequest request) =>
        (long)(request.RackTo - request.RackFrom + 1) * (request.BinFrom is null ? 1 : request.BinTo!.Value - request.BinFrom.Value + 1);
}

public interface IWarehouseLocationBulkService
{
    /// <summary>Aralıktaki adresleri açar; depoda zaten var olan kodlar atlanır.</summary>
    Task<Result<BulkLocationResultDto>> CreateAsync(BulkLocationRequest request, CancellationToken cancellationToken);
}

public sealed class WarehouseLocationBulkService(IKonfeksiyonDbContext db) : IWarehouseLocationBulkService
{
    public async Task<Result<BulkLocationResultDto>> CreateAsync(BulkLocationRequest request, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Warehouses, request.WarehouseId, null, "warehouseId", "Depo", cancellationToken) is { } error)
        {
            return error;
        }

        var zone = request.Zone.Trim().ToUpperInvariant();
        var format = "D" + request.Digits;
        var bins = request.BinFrom is { } from
            ? Enumerable.Range(from, request.BinTo!.Value - from + 1).Select(bin => (string?)bin.ToString(format)).ToList()
            : [null];

        var existing = (await db.WarehouseLocations
                .AsNoTracking()
                .Where(l => l.WarehouseId == request.WarehouseId)
                .Select(l => l.Code)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = new List<WarehouseLocation>();
        var skipped = 0;
        for (var rack = request.RackFrom; rack <= request.RackTo; rack++)
        {
            foreach (var bin in bins)
            {
                var location = new WarehouseLocation { WarehouseId = request.WarehouseId };
                location.SetAddress(zone, rack.ToString(format), bin);
                if (existing.Add(location.Code))
                {
                    created.Add(location);
                }
                else
                {
                    skipped++;
                }
            }
        }

        db.WarehouseLocations.AddRange(created);
        await db.SaveChangesAsync(cancellationToken);
        return new BulkLocationResultDto(created.Count, skipped, created.FirstOrDefault()?.Code, created.LastOrDefault()?.Code);
    }
}
