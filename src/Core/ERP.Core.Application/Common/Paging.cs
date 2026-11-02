using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Application.Common;

/// <summary>Liste uçlarının ortak sayfalama parametreleri. Geçersiz değerler sınırlara çekilir.</summary>
public class PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>Sıralanacak liste alanı; azalan sıra için başına "-" konur (örn. <c>-createdAt</c>).</summary>
    public string? Sort { get; set; }

    /// <summary>Sütun filtreleri, liste alanı adıyla (sorgu dizgisinde <c>filter[orderNo]=IE-2026</c>).</summary>
    public Dictionary<string, string>? Filter { get; set; }

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize,
    };
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public static class PagingExtensions
{
    /// <summary>
    /// Sorgu sıralanmış olmalıdır; aksi halde sayfalar arasında kayıtlar yer değiştirebilir. İstekteki sütun
    /// filtreleri ve sıralama (<see cref="GridQuery"/>) sayfalamadan önce uygulanır.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, PageRequest request, CancellationToken cancellationToken)
    {
        query = GridQuery.Apply(query, request.Sort, request.Filter);

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize;

        var totalCount = await query.CountAsync(cancellationToken);
        var items = totalCount == 0
            ? []
            : await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}
