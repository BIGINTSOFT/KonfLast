using System.Text.RegularExpressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Entities.Identity;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Application.Features.Workspace;

public sealed record FormDraftDto(string FormKey, string Payload, string? Title, DateTime UpdatedAt);

/// <param name="Payload">Formun JSON durumu (sunucu yorumlamaz).</param>
public sealed record SaveFormDraftRequest(string Payload, string? Title = null);

public sealed partial class SaveFormDraftRequestValidator : AbstractValidator<SaveFormDraftRequest>
{
    public SaveFormDraftRequestValidator()
    {
        RuleFor(x => x.Payload)
            .NotEmpty().WithMessage("Taslak içeriği boş olamaz.")
            .MaximumLength(UserFormDraft.MaxPayloadLength).WithMessage("Taslak çok büyük.");
        RuleFor(x => x.Title).MaximumLength(200);
    }

    /// <summary>Form anahtarı: küçük harf, rakam, "-", ":" (örn. sales-order:new).</summary>
    public static bool IsValidKey(string key) => key.Length <= UserFormDraft.KeyMaxLength && KeyPattern().IsMatch(key);

    [GeneratedRegex("^[a-z0-9][a-z0-9:-]*$")]
    private static partial Regex KeyPattern();
}

public interface IFormDraftService
{
    Task<Result<FormDraftDto?>> GetAsync(string formKey, CancellationToken cancellationToken);
    Task<Result<FormDraftDto>> SaveAsync(string formKey, SaveFormDraftRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(string formKey, CancellationToken cancellationToken);
}

/// <summary>Kullanıcıya özel form taslakları (kaydedilmemiş girişlerin sunucuda saklanması).</summary>
public sealed class FormDraftService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IFormDraftService
{
    private static readonly Error InvalidKey = ReferenceValidation.InvalidReference("formKey", "Geçersiz form anahtarı.");
    private static readonly Error NoUser = new(ErrorCodes.Forbidden, "Taslak yalnızca kullanıcı oturumunda saklanır.", ErrorType.Forbidden);

    public async Task<Result<FormDraftDto?>> GetAsync(string formKey, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return NoUser;
        }

        if (!SaveFormDraftRequestValidator.IsValidKey(formKey))
        {
            return InvalidKey;
        }

        return await db.UserFormDrafts.AsNoTracking()
            .Where(d => d.UserId == userId && d.FormKey == formKey)
            .Select(d => new FormDraftDto(d.FormKey, d.Payload, d.Title, d.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<FormDraftDto>> SaveAsync(string formKey, SaveFormDraftRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return NoUser;
        }

        if (!SaveFormDraftRequestValidator.IsValidKey(formKey))
        {
            return InvalidKey;
        }

        var draft = await db.UserFormDrafts.SingleOrDefaultAsync(d => d.UserId == userId && d.FormKey == formKey, cancellationToken);
        if (draft is null)
        {
            // Sınır aşılırsa en eski taslak silinir; taslaklar yardımcı bilgidir, kayıt değildir.
            var drafts = await db.UserFormDrafts.Where(d => d.UserId == userId).OrderBy(d => d.UpdatedAt).ToListAsync(cancellationToken);
            db.UserFormDrafts.RemoveRange(drafts.Take(Math.Max(0, drafts.Count - UserFormDraft.MaxPerUser + 1)));
            draft = new UserFormDraft { UserId = userId, FormKey = formKey };
            db.UserFormDrafts.Add(draft);
        }

        draft.Payload = request.Payload;
        draft.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        draft.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return new FormDraftDto(draft.FormKey, draft.Payload, draft.Title, draft.UpdatedAt);
    }

    public async Task<Result> DeleteAsync(string formKey, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return NoUser;
        }

        await db.UserFormDrafts.Where(d => d.UserId == userId && d.FormKey == formKey).ExecuteDeleteAsync(cancellationToken);
        return Result.Success();
    }
}
