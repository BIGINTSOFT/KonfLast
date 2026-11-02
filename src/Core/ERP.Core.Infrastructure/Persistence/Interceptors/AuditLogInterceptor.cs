using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Core.Application.Abstractions;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Entities;
using ERP.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace ERP.Core.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Entity değişikliklerini AuditLogs tablosuna yazar (kim, ne, ne zaman, hangi cihaz, eski/yeni değer).
///
/// Audit satırları asıl değişiklikle AYNI transaction içinde kaydedilir. Yeni eklenen kayıtların
/// Id'si veritabanında üretildiği için onların audit satırları ilk kayıttan sonra, ikinci bir
/// SaveChanges ile yazılır; çağıran taraf transaction açmamışsa ikisini kapsayan transaction'ı
/// bu interceptor açar ve kapatır.
///
/// Not: ExecuteUpdate / ExecuteDelete ile yapılan toplu işlemler change tracker'dan geçmez
/// ve burada görünmez.
/// </summary>
public sealed class AuditLogInterceptor(ICurrentUser currentUser, TimeProvider timeProvider) : SaveChangesInterceptor
{
    private const string SystemUser = "system";
    private const string MaskedValue = "***";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) },
    };

    private readonly List<CapturedChange> _awaitingKeys = [];
    private IDbContextTransaction? _ownedTransaction;
    private bool _writingAuditRows;

    private sealed record CapturedChange(
        EntityEntry Entry,
        AuditAction Action,
        Dictionary<string, object?>? OldValues,
        Dictionary<string, object?>? NewValues)
    {
        /// <summary>Değeri veritabanında üretilecek alanlar; kayıttan sonra NewValues'a eklenir.</summary>
        public IReadOnlyList<string> GeneratedProperties { get; init; } = [];
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is { } context && !_writingAuditRows && Capture(context))
        {
            _ownedTransaction = context.Database.BeginTransaction();
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context && !_writingAuditRows && Capture(context))
        {
            _ownedTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is { } context && !_writingAuditRows)
        {
            try
            {
                if (StageAwaitingRows(context))
                {
                    SaveAuditRows(() => context.SaveChanges());
                }

                var transaction = TakeOwnedTransaction();
                transaction?.Commit();
                transaction?.Dispose();
            }
            catch
            {
                TakeOwnedTransaction()?.Dispose();
                throw;
            }
        }

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context && !_writingAuditRows)
        {
            try
            {
                if (StageAwaitingRows(context))
                {
                    _writingAuditRows = true;
                    try
                    {
                        await context.SaveChangesAsync(cancellationToken);
                    }
                    finally
                    {
                        _writingAuditRows = false;
                    }
                }

                if (TakeOwnedTransaction() is { } transaction)
                {
                    await transaction.CommitAsync(cancellationToken);
                    await transaction.DisposeAsync();
                }
            }
            catch
            {
                // Dispose, commit edilmemiş transaction'ı geri alır.
                if (TakeOwnedTransaction() is { } transaction)
                {
                    await transaction.DisposeAsync();
                }

                throw;
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (!_writingAuditRows)
        {
            _awaitingKeys.Clear();
            TakeOwnedTransaction()?.Dispose();
        }

        base.SaveChangesFailed(eventData);
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (!_writingAuditRows)
        {
            _awaitingKeys.Clear();
            if (TakeOwnedTransaction() is { } transaction)
            {
                await transaction.DisposeAsync();
            }
        }

        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    /// <summary>
    /// Değişiklikleri toplar. Anahtarı belli olanların audit satırı hemen context'e eklenir;
    /// anahtarı kayıttan sonra oluşacaklar bekletilir. Bekleyen varsa ve açık transaction yoksa
    /// true döner (çağıran transaction açmalıdır).
    /// </summary>
    private bool Capture(DbContext context)
    {
        _awaitingKeys.Clear();

        // Entries() bekleyen değişiklikleri algılar; ToList: döngü içinde context'e satır ekleniyor.
        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)
                || entry.Metadata.ClrType.IsDefined(typeof(NotAuditedAttribute), inherit: false))
            {
                continue;
            }

            if (Describe(entry) is not { } change)
            {
                continue;
            }

            if (entry.Properties.Any(p => p.Metadata.IsPrimaryKey() && p.IsTemporary))
            {
                _awaitingKeys.Add(change);
            }
            else
            {
                context.Add(ToAuditLog(change));
            }
        }

        return _awaitingKeys.Count > 0 && context.Database.CurrentTransaction is null;
    }

    private bool StageAwaitingRows(DbContext context)
    {
        if (_awaitingKeys.Count == 0)
        {
            return false;
        }

        context.AddRange(_awaitingKeys.Select(ToAuditLog));
        _awaitingKeys.Clear();
        return true;
    }

    private void SaveAuditRows(Action save)
    {
        _writingAuditRows = true;
        try
        {
            save();
        }
        finally
        {
            _writingAuditRows = false;
        }
    }

    private IDbContextTransaction? TakeOwnedTransaction()
    {
        var transaction = _ownedTransaction;
        _ownedTransaction = null;
        return transaction;
    }

    private static CapturedChange? Describe(EntityEntry entry)
    {
        var properties = entry.Properties.Where(IsAudited).ToList();

        switch (entry.State)
        {
            case EntityState.Added:
                return new CapturedChange(
                    entry, AuditAction.Created, null,
                    properties.Where(p => !p.IsTemporary).ToDictionary(Name, p => Value(p, p.CurrentValue)))
                {
                    GeneratedProperties = properties.Where(p => p.IsTemporary).Select(Name).ToList(),
                };

            case EntityState.Deleted:
                return new CapturedChange(
                    entry, AuditAction.Deleted, properties.ToDictionary(Name, p => Value(p, p.OriginalValue)), null);

            default:
                // Soft delete bir güncelleme olarak gelir ama anlamı silmedir.
                if (entry.Entity is ISoftDeletable { IsDeleted: true }
                    && entry.Property(nameof(ISoftDeletable.IsDeleted)).OriginalValue is false)
                {
                    return new CapturedChange(
                        entry, AuditAction.Deleted,
                        properties.ToDictionary(Name, p => Value(p, p.OriginalValue)), null);
                }

                var changed = properties
                    .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
                    .ToList();

                return changed.Count == 0
                    ? null
                    : new CapturedChange(
                        entry, AuditAction.Updated,
                        changed.ToDictionary(Name, p => Value(p, p.OriginalValue)),
                        changed.ToDictionary(Name, p => Value(p, p.CurrentValue)));
        }
    }

    private AuditLog ToAuditLog(CapturedChange change)
    {
        var entry = change.Entry;
        foreach (var name in change.GeneratedProperties)
        {
            change.NewValues![name] = entry.Property(name).CurrentValue;
        }

        var keyValues = entry.Metadata.FindPrimaryKey()!.Properties
            .Select(p => Convert.ToString(entry.Property(p.Name).CurrentValue, System.Globalization.CultureInfo.InvariantCulture));

        return new AuditLog
        {
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = string.Join(",", keyValues),
            Action = change.Action,
            OldValues = change.OldValues is null ? null : JsonSerializer.Serialize(change.OldValues, JsonOptions),
            NewValues = change.NewValues is null ? null : JsonSerializer.Serialize(change.NewValues, JsonOptions),
            UserId = currentUser.UserId,
            UserName = currentUser.UserName ?? SystemUser,
            IpAddress = currentUser.IpAddress,
            DeviceInfo = currentUser.DeviceInfo,
            CorrelationId = currentUser.CorrelationId,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
    }

    private static bool IsAudited(PropertyEntry property) =>
        property.Metadata.ClrType != typeof(byte[])
        && property.Metadata.PropertyInfo?.IsDefined(typeof(AuditIgnoreAttribute)) != true;

    private static string Name(PropertyEntry property) => property.Metadata.Name;

    private static object? Value(PropertyEntry property, object? value) =>
        property.Metadata.PropertyInfo?.IsDefined(typeof(AuditMaskedAttribute)) == true ? MaskedValue : value;
}
