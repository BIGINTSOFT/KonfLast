using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Entities.Identity;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Application.Features.Workspace;

/// <param name="Paths">Ana sayfa kısayolları: ekran adresleri, gösterilecekleri sırayla.</param>
public sealed record SaveShortcutsRequest(IReadOnlyList<string> Paths);

public sealed class SaveShortcutsRequestValidator : AbstractValidator<SaveShortcutsRequest>
{
    public SaveShortcutsRequestValidator()
    {
        RuleFor(x => x.Paths).NotNull().WithMessage("Kısayol listesi zorunludur.");
        When(x => x.Paths is not null, () =>
        {
            RuleFor(x => x.Paths)
                .Must(paths => paths.Count <= UserShortcut.MaxPerUser)
                .WithMessage($"En fazla {UserShortcut.MaxPerUser} kısayol eklenebilir.")
                .Must(paths => paths.All(IsScreenPath)).WithMessage("Kısayol, uygulama içindeki bir ekranın adresi olmalıdır.")
                .Must(paths => paths.Distinct(StringComparer.Ordinal).Count() == paths.Count)
                .WithMessage("Aynı ekran birden fazla kez eklenemez.");
        });
    }

    // Yalnızca uygulama içi adres: dış bağlantı ya da betik saklanamaz.
    private static bool IsScreenPath(string? path) =>
        path is { Length: > 0 and <= UserShortcut.PathMaxLength } && path[0] == '/'
        && path.All(c => char.IsAsciiLetterOrDigit(c) || c is '/' or '-');
}

public interface IShortcutService
{
    /// <summary>Oturumdaki kullanıcının ana sayfa kısayolları (ekran adresleri), sırasıyla.</summary>
    Task<IReadOnlyList<string>> GetShortcutsAsync(CancellationToken cancellationToken);

    /// <summary>Kullanıcının kısayollarını verilen listeyle değiştirir.</summary>
    Task<Result<IReadOnlyList<string>>> SaveShortcutsAsync(SaveShortcutsRequest request, CancellationToken cancellationToken);
}

/// <summary>Kullanıcının ana sayfa kısayolları (hangi modüller kurulu olursa olsun çalışır).</summary>
public sealed class ShortcutService(IAppDbContext db, ICurrentUser currentUser) : IShortcutService
{
    public async Task<IReadOnlyList<string>> GetShortcutsAsync(CancellationToken cancellationToken)
    {
        // Tablet oturumunun (cihaz token'ı) kullanıcısı ve ana sayfası yoktur.
        if (currentUser.UserId is not { } userId)
        {
            return [];
        }

        return await db.UserShortcuts
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.SortOrder)
            .Select(s => s.Path)
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<IReadOnlyList<string>>> SaveShortcutsAsync(
        SaveShortcutsRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return new Error(ErrorCodes.Forbidden, "Kısayollar yalnızca kullanıcı oturumunda kaydedilebilir.", ErrorType.Forbidden);
        }

        var existing = await db.UserShortcuts.Where(s => s.UserId == userId).ToListAsync(cancellationToken);
        db.UserShortcuts.RemoveRange(existing.Where(s => !request.Paths.Contains(s.Path)));
        for (var order = 0; order < request.Paths.Count; order++)
        {
            var shortcut = existing.FirstOrDefault(s => s.Path == request.Paths[order]);
            if (shortcut is null)
            {
                shortcut = new UserShortcut { UserId = userId, Path = request.Paths[order] };
                db.UserShortcuts.Add(shortcut);
            }

            shortcut.SortOrder = order;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result<IReadOnlyList<string>>.Success(request.Paths.ToList());
    }
}
