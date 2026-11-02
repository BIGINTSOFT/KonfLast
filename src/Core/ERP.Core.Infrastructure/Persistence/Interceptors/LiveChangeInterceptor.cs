using System.Data.Common;
using ERP.Core.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ERP.Core.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Kaydedilen değişikliklerin hangi canlı alanları (<see cref="LiveTopic"/>) etkilediğini çıkarır ve
/// değişiklik KALICI olduktan sonra duyurur: transaction varsa commit'ten sonra, yoksa kayıttan hemen sonra.
/// Geri alınan işlem duyurulmaz. Böylece servislerin tek tek bildirim göndermesi gerekmez ve hiçbiri unutulmaz.
///
/// Not: ExecuteUpdate / ExecuteDelete change tracker'dan geçmez ve burada görünmez; canlı ekranı
/// etkileyen işlemler her zaman en az bir izlenen kayıt (olay, durum kaydı) ekler.
/// </summary>
public sealed class LiveChangeInterceptor(ILiveNotifier notifier, IEnumerable<ILiveTopicSource> topicSources) : DbTransactionInterceptor, ISaveChangesInterceptor
{
    /// <summary>Kurulu modüllerin bildirdiği entity → canlı alan eşleşmeleri.</summary>
    private readonly Dictionary<Type, LiveTopic> _topics = topicSources
        .SelectMany(s => s.Topics)
        .ToDictionary(p => p.Key, p => p.Value);

    /// <summary>Süren SaveChanges'in etkilediği alanlar (kayıt başarısız olursa atılır).</summary>
    private readonly HashSet<LiveTopic> _staged = [];

    /// <summary>Kaydedilmiş ama transaction'ı henüz commit edilmemiş alanlar.</summary>
    private readonly HashSet<LiveTopic> _saved = [];

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

    public void SaveChangesFailed(DbContextErrorEventData eventData) => _staged.Clear();

    public Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _staged.Clear();
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
        _saved.Clear();

    public override Task TransactionRolledBackAsync(
        DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        _saved.Clear();
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
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                && _topics.TryGetValue(entry.Metadata.ClrType, out var topic))
            {
                _staged.Add(topic);
            }
        }
    }

    private void Saved(DbContext? context)
    {
        _saved.UnionWith(_staged);
        _staged.Clear();

        if (context?.Database.CurrentTransaction is null)
        {
            Flush();
        }
    }

    private void Flush()
    {
        if (_saved.Count == 0)
        {
            return;
        }

        var topics = _saved.ToArray();
        _saved.Clear();
        notifier.Publish(topics);
    }
}
