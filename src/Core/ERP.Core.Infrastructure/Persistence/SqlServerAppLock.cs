using System.Data;
using ERP.Core.Application.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Infrastructure.Persistence;

/// <summary>SQL Server uygulama kilidi (sp_getapplock); kilit transaction bitince kendiliğinden bırakılır.</summary>
public sealed class SqlServerAppLock(AppDbContext db) : IAppLock
{
    public async Task<bool> TryAcquireAsync(string resource, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Uygulama kilidi bir transaction içinde alınmalıdır.");
        }

        var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };

        await db.Database.ExecuteSqlRawAsync(
            "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', " +
            "@LockOwner = 'Transaction', @LockTimeout = @timeout",
            [
                result,
                new SqlParameter("@resource", resource),
                new SqlParameter("@timeout", (int)timeout.TotalMilliseconds),
            ],
            cancellationToken);

        // 0: alındı, 1: beklendikten sonra alındı; negatif değerler zaman aşımı / iptal / deadlock.
        return (int)result.Value >= 0;
    }
}
