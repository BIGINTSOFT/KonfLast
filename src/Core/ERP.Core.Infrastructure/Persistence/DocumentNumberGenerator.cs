using ERP.Core.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Infrastructure.Persistence;

public sealed class DocumentNumberGenerator(AppDbContext db) : IDocumentNumberGenerator
{
    public Task<int> NextAsync(string series, int year, CancellationToken cancellationToken) =>
        NextRangeAsync(series, year, 1, cancellationToken);

    public async Task<int> NextRangeAsync(string series, int year, int count, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Belge numarası, numarayı kullanan kayıtla aynı transaction içinde alınmalıdır.");
        }

        // Tek ifade: satır yoksa oluşturur, varsa artırır. UPDLOCK + HOLDLOCK aynı seri/yıl için
        // eşzamanlı istekleri (satır henüz yokken bile) sıraya sokar; kilit transaction bitene kadar
        // tutulur, böylece transaction geri alınırsa numara da geri alınır (boşluk oluşmaz).
        var values = await db.Database
            .SqlQuery<int>($"""
                MERGE [Core].[DocumentSequences] WITH (UPDLOCK, HOLDLOCK) AS target
                USING (SELECT {series} AS [Series], {year} AS [Year]) AS source
                    ON target.[Series] = source.[Series] AND target.[Year] = source.[Year]
                WHEN MATCHED THEN UPDATE SET [LastValue] = target.[LastValue] + {count}
                WHEN NOT MATCHED THEN INSERT ([Series], [Year], [LastValue]) VALUES (source.[Series], source.[Year], {count})
                OUTPUT inserted.[LastValue] AS [Value];
                """)
            .ToListAsync(cancellationToken);

        // Ayrılan aralığın ilk numarası.
        return values.Single() - count + 1;
    }
}
