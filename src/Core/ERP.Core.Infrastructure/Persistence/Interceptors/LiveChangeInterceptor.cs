using System.Data.Common;
using ERP.Core.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ERP.Core.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Kaydedilen değişikliklerin hangi canlı alanları (<see cref="LiveTopic"/>) ve hangi makine / tabletleri
/// (<see cref="LiveTarget"/>) etkilediğini çıkarır ve değişiklik KALICI olduktan sonra duyurur: transaction varsa
/// commit'ten sonra, yoksa kayıttan hemen sonra. Geri alınan işlem duyurulmaz. Böylece servislerin tek tek bildirim
/// göndermesi gerekmez ve hiçbiri unutulmaz.
///
/// Not: ExecuteUpdate / ExecuteDelete change tracker'dan geçmez ve burada görünmez; canlı ekranı
/// etkileyen işlemler her zaman en az bir izlenen kayıt (olay, durum kaydı) ekler.
/// </summary>
public sealed class LiveChangeInterceptor(
    ILiveNotifier notifier,
    IEnumerable<ILiveTopicSource> topicSources,
    IEnumerable<ILiveTargetSource> targetSources) : DbTransactionInterceptor, ISaveChangesInterceptor
{
    /// <summary>Kurulu modüllerin bildirdiği entity → canlı alan eşleşmeleri.</summary>
    private readonly Dictionary<Type, LiveTopic> _topics = topicSources
        .SelectMany(s => s.Topics)
        .ToDictionary(p => p.Key, p => p.Value);

    private readonly ILiveTargetSource[] _targetSources = [.. targetSources];

    /// <summary>Süren SaveChanges'in etkilediği alanlar ve hedefler (kayıt başarısız olursa atılır).</summary>
    private readonly HashSet<LiveTopic> _staged = [];
    private readonly HashSet<LiveTarget> _stagedTargets = [];

    /// <summary>Kaydedilmiş ama transaction'ı henüz commit edilmemiş alanlar ve hedefler.</summary>
    private readonly HashSet<LiveTopic> _saved = [];
    private readonly HashSet<LiveTarget> _savedTargets = [];

    public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stage(eventData.Context);
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stage(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Saved(eventData.Context);
        return result;
    }

    public ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Saved(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public void SaveChangesFailed(DbContextErrorEventData eventData) => ClearStaged();

    public Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        ClearStaged();
        return Task.CompletedTask;
    }

    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData) => Flush();

    public override Task TransactionCommittedAsync(
        DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Flush();
        return Task.CompletedTask;
    }

    public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData) =>
        ClearSaved();

    public override Task TransactionRolledBackAsync(
        DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        ClearSaved();
        return Task.CompletedTask;
    }

    private void Stage(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (_topics.TryGetValue(entry.Metadata.ClrType, out var topic))
            {
                _staged.Add(topic);
            }

            foreach (var source in _targetSources)
            {
                _stagedTargets.UnionWith(source.TargetsOf(entry.Entity));
            }
        }
    }

    private void Saved(DbContext? context)
    {
        _saved.UnionWith(_staged);
        _savedTargets.UnionWith(_stagedTargets);
        ClearStaged();

        if (context?.Database.CurrentTransaction is null)
        {
            Flush();
        }
    }

    private void Flush()
    {
        if (_saved.Count == 0 && _savedTargets.Count == 0)
        {
            return;
        }

        var topics = _saved.ToArray();
        var targets = _savedTargets.ToArray();
        ClearSaved();
        notifier.Publish(topics, targets);
    }

    private void ClearStaged()
    {
        _staged.Clear();
        _stagedTargets.Clear();
    }

    private void ClearSaved()
    {
        _saved.Clear();
        _savedTargets.Clear();
    }
}
