using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Konfeksiyon.Application.Abstractions;
using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

using ERP.Core.Application.Features.MasterData;

namespace ERP.Konfeksiyon.Application.Features.MasterData;

public sealed record MachineTypeDto(int Id, string Code, string Name, bool IsActive);

public sealed record MachineTypeRequest(string Code, string Name, bool IsActive = true);

public sealed class MachineTypeRequestValidator : AbstractValidator<MachineTypeRequest>
{
    public MachineTypeRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
    }
}

public sealed class MachineTypeService(IKonfeksiyonDbContext db)
    : KonfeksiyonMasterDataService<MachineType, MachineTypeDto, MachineTypeRequest>(db)
{
    protected override DbSet<MachineType> Set => Db.MachineTypes;
    protected override string Label => "Makine tipi";
    protected override string CodeField => "code";

    protected override string CodeOf(MachineTypeRequest request) => request.Code;
    protected override Expression<Func<MachineType, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<MachineType, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<MachineType> Order(IQueryable<MachineType> query) => query.OrderBy(e => e.Code);

    protected override Expression<Func<MachineType, MachineTypeDto>> ToDto =>
        e => new MachineTypeDto(e.Id, e.Code, e.Name, e.IsActive);

    protected override Expression<Func<MachineType, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(MachineTypeRequest request, MachineType entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.Machines.AnyAsync(m => m.MachineTypeId == id, cancellationToken))
        {
            return "makineler";
        }

        return await Db.Operations.AnyAsync(o => o.MachineTypeId == id, cancellationToken) ? "operasyonlar" : null;
    }
}

public sealed record OperationDto(
    int Id, string OperationCode, string Name, string? Description, decimal Sam, int? MachineTypeId,
    string? MachineTypeName, bool IsActive, decimal? PieceRate = null)
{
    /// <summary>Teorik kapasite: %100 verimde bir saatte yapılabilecek adet (60 ÷ SAM). SAM 0 ise boş.</summary>
    public decimal? CapacityPerHour => Sam > 0 ? Math.Round(60 / Sam, 1) : null;
}

/// <param name="Sam">Standart süre, dakika / adet.</param>
/// <param name="MachineTypeId">Operasyonun gerektirdiği makine tipi (yoksa her makinede yapılabilir).</param>
/// <param name="PieceRate">Parça başı birim ücret (adet başına); boşsa ücret SAM × dakika ücretinden hesaplanır.</param>
public sealed record OperationRequest(
    string OperationCode, string Name, string? Description, decimal Sam, int? MachineTypeId, bool IsActive = true,
    decimal? PieceRate = null);

public sealed class OperationRequestValidator : AbstractValidator<OperationRequest>
{
    public OperationRequestValidator()
    {
        RuleFor(x => x.OperationCode).ValidCode();
        RuleFor(x => x.Name).ValidName(150);
        RuleFor(x => x.Description).OptionalText(500);
        RuleFor(x => x.Sam)
            .InclusiveBetween(0, 9999).WithMessage("SAM 0 ile 9999 dakika arasında olmalıdır.")
            .PrecisionScale(9, 4, ignoreTrailingZeros: true).WithMessage("SAM en fazla 4 ondalık basamak içerebilir.");
        RuleFor(x => x.PieceRate)
            .InclusiveBetween(0, 100_000).WithMessage("Birim ücret 0 ile 100.000 arasında olmalıdır.")
            .PrecisionScale(18, 4, ignoreTrailingZeros: true).WithMessage("Birim ücret en fazla 4 ondalık basamak içerebilir.");
    }
}

public sealed class OperationService(IKonfeksiyonDbContext db, ICurrentPermissions permissions)
    : KonfeksiyonMasterDataService<Operation, OperationDto, OperationRequest>(db)
{
    /// <summary>Parça başı ücreti değiştirme yetkisi yoksa mevcut ücret korunur (gizli alan boş gelir).</summary>
    private bool _keepPieceRate;

    protected override DbSet<Operation> Set => Db.Operations;
    protected override string Label => "Operasyon";
    protected override string CodeField => "operationCode";

    protected override string CodeOf(OperationRequest request) => request.OperationCode;
    protected override Expression<Func<Operation, bool>> WithCode(string code) => e => e.OperationCode == code;

    protected override Expression<Func<Operation, bool>> Matches(string term) =>
        e => e.OperationCode.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<Operation> Order(IQueryable<Operation> query) =>
        query.OrderBy(e => e.OperationCode);

    protected override Expression<Func<Operation, OperationDto>> ToDto => e => new OperationDto(
        e.Id, e.OperationCode, e.Name, e.Description, e.Sam, e.MachineTypeId, e.MachineType!.Name, e.IsActive, e.PieceRate);

    protected override Expression<Func<Operation, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.OperationCode, e.Name);

    protected override void Apply(OperationRequest request, Operation entity, string code)
    {
        entity.OperationCode = code;
        entity.Name = request.Name.Trim();
        entity.Description = NullIfBlank(request.Description);
        entity.Sam = request.Sam;
        if (!_keepPieceRate)
        {
            entity.PieceRate = request.PieceRate;
        }
        entity.MachineTypeId = request.MachineTypeId;
        entity.IsActive = request.IsActive;
    }

    // Parça başı ücret bordroyu doğrudan belirler: görmek ücret görüntüleme, değiştirmek ücret yönetimi ister.
    protected override async Task<Func<OperationDto, OperationDto>?> PresenterAsync(CancellationToken cancellationToken) =>
        await permissions.HasAsync(KonfeksiyonPermissions.Payroll.View, cancellationToken)
            ? null
            : dto => dto with { PieceRate = null };

    protected override async Task<Error?> ValidateAsync(
        OperationRequest request, Operation? existing, CancellationToken cancellationToken)
    {
        _keepPieceRate = !await permissions.HasAsync(KonfeksiyonPermissions.Payroll.Manage, cancellationToken);
        if (_keepPieceRate && request.PieceRate is not null && request.PieceRate != existing?.PieceRate)
        {
            const string message = "Parça başı ücreti değiştirme yetkiniz yok (Ücret ve prim ayarlarını düzenleme).";
            return new Error(
                ErrorCodes.Forbidden, message, ErrorType.Forbidden,
                new Dictionary<string, string[]> { ["pieceRate"] = [message] });
        }

        var referenceError = await RequireSelectableAsync(
            Db.MachineTypes, request.MachineTypeId, existing?.MachineTypeId, "machineTypeId", "Makine tipi",
            cancellationToken);
        if (referenceError is not null)
        {
            return referenceError;
        }

        // Operasyonun makine tipi değişirse, bu operasyona atanmış makineler uyumsuz kalmamalı.
        if (existing is not null && request.MachineTypeId is { } requiredTypeId
            && (await Db.Machines.AnyAsync(
                    m => m.OperationId == existing.Id && m.MachineTypeId != requiredTypeId, cancellationToken)
                || await Db.MachineOperations.AnyAsync(
                    o => o.OperationId == existing.Id && !o.Machine.IsDeleted
                        && o.Machine.MachineTypeId != requiredTypeId,
                    cancellationToken)))
        {
            return new Error(
                ErrorCodes.MachineTypeMismatch,
                "Bu operasyona atanmış farklı tipte makineler var; önce makine atamalarını değiştirin.");
        }

        return null;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.Machines.AnyAsync(m => m.OperationId == id, cancellationToken)
            || await Db.MachineOperations.AnyAsync(o => o.OperationId == id && !o.Machine.IsDeleted, cancellationToken))
        {
            return "makineler";
        }

        return await Db.ProductionRouteOperations.AnyAsync(o => o.OperationId == id, cancellationToken)
            ? "rotalar"
            : null;
    }
}

/// <param name="ProductionLineCode">Makinenin şu an bulunduğu bant (banta atanmamışsa boş).</param>
/// <param name="NextMaintenanceDate">Son bakım tarihi ve bakım aralığından hesaplanan sonraki bakım tarihi.</param>
public sealed record MachineDto(
    int Id,
    string MachineCode,
    string MachineName,
    int MachineTypeId,
    string MachineTypeName,
    int? OperationId,
    string? OperationName,
    MachineStatus Status,
    string? ProductionLineCode,
    bool IsActive,
    string? Brand,
    string? ModelNo,
    string? SerialNo,
    int? ManufactureYear,
    string? AssetNo,
    DateOnly? PurchaseDate,
    int? SupplierId,
    string? SupplierName,
    DateOnly? WarrantyEndDate,
    int? MaintenanceIntervalDays,
    DateOnly? LastMaintenanceDate,
    DateOnly? NextMaintenanceDate,
    string? Notes);

/// <param name="OperationId">Makinenin ana operasyonu (henüz atanmamışsa boş); ek operasyonlar ayrıca tanımlanır.</param>
/// <param name="MaintenanceIntervalDays">Periyodik bakım aralığı (gün).</param>
public sealed record MachineRequest(
    string MachineCode,
    string MachineName,
    int MachineTypeId,
    int? OperationId,
    bool IsActive = true,
    string? Brand = null,
    string? ModelNo = null,
    string? SerialNo = null,
    int? ManufactureYear = null,
    string? AssetNo = null,
    DateOnly? PurchaseDate = null,
    int? SupplierId = null,
    DateOnly? WarrantyEndDate = null,
    int? MaintenanceIntervalDays = null,
    DateOnly? LastMaintenanceDate = null,
    string? Notes = null);

public sealed class MachineRequestValidator : AbstractValidator<MachineRequest>
{
    public MachineRequestValidator(TimeProvider clock)
    {
        RuleFor(x => x.MachineCode).ValidCode();
        RuleFor(x => x.MachineName).ValidName(150);
        RuleFor(x => x.MachineTypeId).GreaterThan(0).WithMessage("Makine tipi seçilmelidir.");
        RuleFor(x => x.Brand).OptionalText(100);
        RuleFor(x => x.ModelNo).OptionalText(100);
        RuleFor(x => x.SerialNo).OptionalText(100);
        RuleFor(x => x.AssetNo).OptionalText(50);
        RuleFor(x => x.Notes).OptionalText(500);
        RuleFor(x => x.ManufactureYear)
            .Must(year => year >= 1950 && year <= clock.GetUtcNow().Year + 1)
            .WithMessage("Üretim yılı 1950 ile gelecek yıl arasında olmalıdır.")
            .When(x => x.ManufactureYear is not null);
        RuleFor(x => x.PurchaseDate)
            .Must(date => date <= DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(1))
            .WithMessage("Satın alma tarihi gelecekte olamaz.")
            .When(x => x.PurchaseDate is not null);
        RuleFor(x => x.WarrantyEndDate)
            .GreaterThanOrEqualTo(x => x.PurchaseDate!.Value).WithMessage("Garanti bitişi satın alma tarihinden önce olamaz.")
            .When(x => x.WarrantyEndDate is not null && x.PurchaseDate is not null);
        RuleFor(x => x.MaintenanceIntervalDays)
            .InclusiveBetween(1, 3650).WithMessage("Bakım aralığı 1–3650 gün arasında olmalıdır.")
            .When(x => x.MaintenanceIntervalDays is not null);
        RuleFor(x => x.LastMaintenanceDate)
            .Must(date => date <= DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(1))
            .WithMessage("Son bakım tarihi gelecekte olamaz.")
            .When(x => x.LastMaintenanceDate is not null);
    }
}

public sealed class MachineService(IKonfeksiyonDbContext db) : KonfeksiyonMasterDataService<Machine, MachineDto, MachineRequest>(db)
{
    protected override DbSet<Machine> Set => Db.Machines;
    protected override string Label => "Makine";
    protected override string CodeField => "machineCode";

    protected override string CodeOf(MachineRequest request) => request.MachineCode;
    protected override Expression<Func<Machine, bool>> WithCode(string code) => e => e.MachineCode == code;

    protected override Expression<Func<Machine, bool>> Matches(string term) =>
        e => e.MachineCode.Contains(term) || e.MachineName.Contains(term);

    protected override IQueryable<Machine> Order(IQueryable<Machine> query) => query.OrderBy(e => e.MachineCode);

    protected override Expression<Func<Machine, MachineDto>> ToDto => e => new MachineDto(
        e.Id, e.MachineCode, e.MachineName, e.MachineTypeId, e.MachineType.Name, e.OperationId, e.Operation!.Name,
        e.Status,
        Db.LineMachineAssignments
            .Where(a => a.MachineId == e.Id && a.UnassignedAt == null)
            .Select(a => a.ProductionLine.LineCode)
            .FirstOrDefault(),
        e.IsActive, e.Brand, e.ModelNo, e.SerialNo, e.ManufactureYear, e.AssetNo, e.PurchaseDate, e.SupplierId,
        e.Supplier!.Name, e.WarrantyEndDate, e.MaintenanceIntervalDays, e.LastMaintenanceDate,
        e.LastMaintenanceDate != null && e.MaintenanceIntervalDays != null
            ? e.LastMaintenanceDate.Value.AddDays(e.MaintenanceIntervalDays.Value)
            : null,
        e.Notes);

    protected override Expression<Func<Machine, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.MachineCode, e.MachineName);

    protected override void Apply(MachineRequest request, Machine entity, string code)
    {
        // Durum (Status) burada değişmez: üretim, arıza ve bakım akışları tarafından yönetilir.
        entity.MachineCode = code;
        entity.MachineName = request.MachineName.Trim();
        entity.MachineTypeId = request.MachineTypeId;
        entity.OperationId = request.OperationId;
        entity.Brand = NullIfBlank(request.Brand);
        entity.ModelNo = NullIfBlank(request.ModelNo);
        entity.SerialNo = NullIfBlank(request.SerialNo);
        entity.ManufactureYear = request.ManufactureYear;
        entity.AssetNo = NullIfBlank(request.AssetNo);
        entity.PurchaseDate = request.PurchaseDate;
        entity.SupplierId = request.SupplierId;
        entity.WarrantyEndDate = request.WarrantyEndDate;
        entity.MaintenanceIntervalDays = request.MaintenanceIntervalDays;
        entity.LastMaintenanceDate = request.LastMaintenanceDate;
        entity.Notes = NullIfBlank(request.Notes);
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(
        MachineRequest request, Machine? existing, CancellationToken cancellationToken)
    {
        var referenceError =
            await RequireSelectableAsync(
                Db.MachineTypes, request.MachineTypeId, existing?.MachineTypeId, "machineTypeId", "Makine tipi",
                cancellationToken)
            ?? await RequireSelectableAsync(
                Db.Operations, request.OperationId, existing?.OperationId, "operationId", "Operasyon",
                cancellationToken)
            ?? await RequireSelectableAsync(
                Db.Suppliers, request.SupplierId, existing?.SupplierId, "supplierId", "Tedarikçi", cancellationToken);
        if (referenceError is not null)
        {
            return referenceError;
        }

        if (existing is not null && existing.MachineTypeId != request.MachineTypeId
            && await Db.MachineOperations.AnyAsync(
                o => o.MachineId == existing.Id && o.Operation.MachineTypeId != null
                    && o.Operation.MachineTypeId != request.MachineTypeId,
                cancellationToken))
        {
            return new Error(
                ErrorCodes.MachineTypeMismatch,
                "Makinenin ek operasyonlarından biri farklı bir makine tipi gerektiriyor; önce ek operasyonları düzenleyin.",
                ErrorType.BusinessRule,
                new Dictionary<string, string[]>
                {
                    ["machineTypeId"] = ["Makinenin ek operasyonlarından biri farklı bir makine tipi gerektiriyor."],
                });
        }

        if (request.OperationId is { } operationId)
        {
            var requiredTypeId = await Db.Operations
                .Where(o => o.Id == operationId)
                .Select(o => o.MachineTypeId)
                .SingleAsync(cancellationToken);

            if (requiredTypeId is not null && requiredTypeId != request.MachineTypeId)
            {
                return new Error(
                    ErrorCodes.MachineTypeMismatch,
                    "Seçilen operasyon farklı bir makine tipi gerektiriyor.",
                    ErrorType.BusinessRule,
                    new Dictionary<string, string[]>
                    {
                        ["operationId"] = ["Seçilen operasyon farklı bir makine tipi gerektiriyor."],
                    });
            }
        }

        return null;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.LineMachineAssignments.AnyAsync(a => a.MachineId == id && a.UnassignedAt == null, cancellationToken))
        {
            return "makine bir bantta; önce banttan çıkarın";
        }

        return await Db.Tablets.AnyAsync(t => t.MachineId == id, cancellationToken)
            ? "makineye bir tablet bağlı"
            : null;
    }
}

/// <param name="RfidUid">Operatörün geçerli personel kartı (yoksa boş).</param>
/// <param name="BreakGroupId">Operatörün mola grubu (yoksa yalnızca herkese açık molalara çıkar).</param>
/// <param name="Role">Sahadaki görevi: OPERATOR, LINE_CHIEF (bant şefi) ya da MAINTENANCE (bakım operatörü).</param>
/// <param name="CanPause">Kendi kartıyla ara verebilir mi (veremiyorsa bant şefi kartıyla onaylar).</param>
/// <param name="CanReportBreakdown">Kendi kartıyla arıza bildirebilir mi.</param>
public sealed record OperatorDto(
    int Id, string OperatorCode, string FirstName, string LastName, string? RfidUid, bool IsActive,
    int? BreakGroupId = null, string? BreakGroupName = null, StaffRole Role = StaffRole.Operator,
    bool CanPause = false, bool CanReportBreakdown = false);

public sealed record OperatorRequest(
    string OperatorCode, string FirstName, string LastName, bool IsActive = true, int? BreakGroupId = null,
    StaffRole Role = StaffRole.Operator, bool CanPause = false, bool CanReportBreakdown = false);

public sealed class OperatorRequestValidator : AbstractValidator<OperatorRequest>
{
    public OperatorRequestValidator()
    {
        RuleFor(x => x.OperatorCode).ValidCode();
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad zorunludur.")
            .MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir.");
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad zorunludur.")
            .MaximumLength(100).WithMessage("Soyad en fazla 100 karakter olabilir.");
        RuleFor(x => x.Role).IsInEnum().WithMessage("Geçersiz görev.");
    }
}

public sealed class OperatorService(IKonfeksiyonDbContext db, ICurrentPermissions permissions)
    : KonfeksiyonMasterDataService<Operator, OperatorDto, OperatorRequest>(db)
{
    protected override DbSet<Operator> Set => Db.Operators;
    protected override string Label => "Operatör";
    protected override string CodeField => "operatorCode";

    protected override string CodeOf(OperatorRequest request) => request.OperatorCode;
    protected override Expression<Func<Operator, bool>> WithCode(string code) => e => e.OperatorCode == code;

    protected override Expression<Func<Operator, bool>> Matches(string term) =>
        e => e.OperatorCode.Contains(term) || e.FirstName.Contains(term) || e.LastName.Contains(term);

    protected override IQueryable<Operator> Order(IQueryable<Operator> query) => query.OrderBy(e => e.OperatorCode);

    protected override Expression<Func<Operator, OperatorDto>> ToDto =>
        e => new OperatorDto(
            e.Id,
            e.OperatorCode,
            e.FirstName,
            e.LastName,
            Db.OperatorCards.Where(c => c.OperatorId == e.Id && c.RevokedAt == null).Select(c => c.RfidCard.Uid).FirstOrDefault(),
            e.IsActive,
            e.BreakGroupId,
            e.BreakGroup!.Name,
            e.Role,
            e.CanPause,
            e.CanReportBreakdown);

    protected override Expression<Func<Operator, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.OperatorCode, e.FirstName + " " + e.LastName);

    protected override void Apply(OperatorRequest request, Operator entity, string code)
    {
        entity.OperatorCode = code;
        entity.FirstName = request.FirstName.Trim();
        entity.LastName = request.LastName.Trim();
        entity.IsActive = request.IsActive;
        entity.BreakGroupId = request.BreakGroupId;
        entity.Role = request.Role;
        entity.CanPause = request.CanPause;
        entity.CanReportBreakdown = request.CanReportBreakdown;
    }

    protected override async Task<Error?> ValidateAsync(
        OperatorRequest request, Operator? existing, CancellationToken cancellationToken)
    {
        // Saha görevi (bant şefi, bakım) ve ara / arıza hakları tablet yetkilerini belirler: ayrı yetki ister.
        var floorChanged = existing is null
            ? request.Role != StaffRole.Operator || request.CanPause || request.CanReportBreakdown
            : request.Role != existing.Role || request.CanPause != existing.CanPause
                || request.CanReportBreakdown != existing.CanReportBreakdown;
        if (floorChanged && !await permissions.HasAsync(KonfeksiyonPermissions.Operator.AssignFloorRole, cancellationToken))
        {
            const string message = "Saha görevini ve ara / arıza haklarını değiştirme yetkiniz yok.";
            return new Error(
                ErrorCodes.Forbidden, message, ErrorType.Forbidden,
                new Dictionary<string, string[]> { ["role"] = [message] });
        }

        return await ReferenceValidation.RequireSelectableAsync(
            Db.BreakGroups, request.BreakGroupId, existing?.BreakGroupId, "breakGroupId", "Mola grubu", cancellationToken);
    }
}
