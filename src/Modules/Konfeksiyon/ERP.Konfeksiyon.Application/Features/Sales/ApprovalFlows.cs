using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Sales;

public sealed record ApprovalUserDto(int Id, string UserName, string FullName);

/// <param name="MinAmount">Yerel para birimi (TRY) cinsinden eşik; boşsa her belgede uygulanır.</param>
public sealed record ApprovalFlowStepDto(
    int Id, int Sequence, string Name, ApproverType ApproverType, int? RoleId, string? RoleName, decimal? MinAmount,
    IReadOnlyList<ApprovalUserDto> Users);

/// <param name="IsDefined">Akış hiç kaydedilmemişse false; belge akışsız onaylanır.</param>
public sealed record ApprovalFlowDto(
    ApprovalDocumentType DocumentType, bool IsDefined, string Name, bool IsActive, IReadOnlyList<ApprovalFlowStepDto> Steps,
    DateTime? UpdatedAt, string? UpdatedBy);

public sealed record ApprovalRoleOptionDto(int Id, string Code, string Name);

/// <summary>Akış düzenleme ekranında seçilebilecek roller ve aktif kullanıcılar.</summary>
public sealed record ApprovalFlowOptionsDto(IReadOnlyList<ApprovalRoleOptionDto> Roles, IReadOnlyList<ApprovalUserDto> Users);

public sealed record ApprovalFlowStepRequest(
    string Name, ApproverType ApproverType, int? RoleId, IReadOnlyList<int>? UserIds, decimal? MinAmount = null);

/// <summary>Akışın tamamı; adımlar verilen sırayla uygulanır.</summary>
public sealed record SaveApprovalFlowRequest(string Name, bool IsActive, IReadOnlyList<ApprovalFlowStepRequest> Steps);

public sealed class SaveApprovalFlowRequestValidator : AbstractValidator<SaveApprovalFlowRequest>
{
    public const int MaxSteps = 10;

    public SaveApprovalFlowRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Akış adı zorunludur.").MaximumLength(100);
        RuleFor(x => x.Steps)
            .NotNull().WithMessage("Adım listesi zorunludur.")
            .Must(steps => steps.Count <= MaxSteps).WithMessage($"En fazla {MaxSteps} adım tanımlanabilir.")
            .Custom((steps, context) =>
            {
                if (steps is null)
                {
                    return;
                }

                for (var i = 0; i < steps.Count; i++)
                {
                    var step = steps[i];
                    if (string.IsNullOrWhiteSpace(step.Name) || step.Name.Trim().Length > 100)
                    {
                        context.AddFailure($"{i + 1}. adım: ad zorunludur (en fazla 100 karakter).");
                    }

                    if (step.ApproverType == ApproverType.Role && step.RoleId is null)
                    {
                        context.AddFailure($"{i + 1}. adım: rol seçilmelidir.");
                    }

                    if (step.ApproverType == ApproverType.Users && (step.UserIds is null || step.UserIds.Count == 0))
                    {
                        context.AddFailure($"{i + 1}. adım: en az bir kişi seçilmelidir.");
                    }

                    if (step.MinAmount is < 0)
                    {
                        context.AddFailure($"{i + 1}. adım: tutar eşiği negatif olamaz.");
                    }
                }
            });
    }
}

public interface IApprovalFlowService
{
    Task<ApprovalFlowDto> GetAsync(ApprovalDocumentType documentType, CancellationToken cancellationToken);
    Task<ApprovalFlowOptionsDto> GetOptionsAsync(CancellationToken cancellationToken);
    Task<Result<ApprovalFlowDto>> SaveAsync(
        ApprovalDocumentType documentType, SaveApprovalFlowRequest request, CancellationToken cancellationToken);
}

/// <summary>Onay akışlarının tanımı (Yönetim). Süren onaylar gönderim anındaki akışla devam eder.</summary>
public sealed class ApprovalFlowService(IKonfeksiyonDbContext db) : IApprovalFlowService
{
    public async Task<ApprovalFlowDto> GetAsync(ApprovalDocumentType documentType, CancellationToken cancellationToken)
    {
        var flow = await db.ApprovalFlows.AsNoTracking()
            .Include(f => f.Steps).ThenInclude(s => s.Role)
            .Include(f => f.Steps).ThenInclude(s => s.Users).ThenInclude(u => u.User)
            .SingleOrDefaultAsync(f => f.DocumentType == documentType, cancellationToken);
        if (flow is null)
        {
            return new ApprovalFlowDto(documentType, false, "Satış siparişi onayı", false, [], null, null);
        }

        var steps = flow.Steps
            .OrderBy(s => s.Sequence)
            .Select(s => new ApprovalFlowStepDto(
                s.Id, s.Sequence, s.Name, s.ApproverType, s.RoleId, s.Role?.Name, s.MinAmount,
                s.Users.Select(u => new ApprovalUserDto(u.UserId, u.User.UserName, u.User.FullName)).OrderBy(u => u.FullName).ToList()))
            .ToList();
        return new ApprovalFlowDto(documentType, true, flow.Name, flow.IsActive, steps, flow.UpdatedAt ?? flow.CreatedAt, flow.UpdatedBy ?? flow.CreatedBy);
    }

    public async Task<ApprovalFlowOptionsDto> GetOptionsAsync(CancellationToken cancellationToken) =>
        new(
            await db.Roles.AsNoTracking().OrderBy(r => r.Name).Select(r => new ApprovalRoleOptionDto(r.Id, r.Code, r.Name))
                .ToListAsync(cancellationToken),
            await db.Users.AsNoTracking().Where(u => u.IsActive).OrderBy(u => u.FullName)
                .Select(u => new ApprovalUserDto(u.Id, u.UserName, u.FullName)).ToListAsync(cancellationToken));

    public async Task<Result<ApprovalFlowDto>> SaveAsync(
        ApprovalDocumentType documentType, SaveApprovalFlowRequest request, CancellationToken cancellationToken)
    {
        var roleIds = request.Steps.Where(s => s.ApproverType == ApproverType.Role).Select(s => s.RoleId!.Value).Distinct().ToList();
        if (await db.Roles.CountAsync(r => roleIds.Contains(r.Id), cancellationToken) != roleIds.Count)
        {
            return ReferenceValidation.InvalidReference("steps", "Seçilen rollerden biri bulunamadı.");
        }

        var userIds = request.Steps.Where(s => s.ApproverType == ApproverType.Users)
            .SelectMany(s => s.UserIds ?? []).Distinct().ToList();
        if (await db.Users.CountAsync(u => userIds.Contains(u.Id) && u.IsActive, cancellationToken) != userIds.Count)
        {
            return ReferenceValidation.InvalidReference("steps", "Seçilen kişilerden biri bulunamadı ya da pasif.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var flow = await db.ApprovalFlows.Include(f => f.Steps).ThenInclude(s => s.Users)
            .SingleOrDefaultAsync(f => f.DocumentType == documentType, cancellationToken);
        if (flow is null)
        {
            flow = new ApprovalFlow { DocumentType = documentType };
            db.ApprovalFlows.Add(flow);
        }
        else
        {
            // Adımlar baştan yazılır; sıra değişikliği tekil (akış, sıra) dizinine takılmasın diye önce silinir.
            db.ApprovalFlowStepUsers.RemoveRange(flow.Steps.SelectMany(s => s.Users));
            db.ApprovalFlowSteps.RemoveRange(flow.Steps);
            await db.SaveChangesAsync(cancellationToken);
        }

        flow.Name = request.Name.Trim();
        flow.IsActive = request.IsActive;
        for (var i = 0; i < request.Steps.Count; i++)
        {
            var item = request.Steps[i];
            var step = new ApprovalFlowStep
            {
                Sequence = i + 1,
                Name = item.Name.Trim(),
                ApproverType = item.ApproverType,
                RoleId = item.ApproverType == ApproverType.Role ? item.RoleId : null,
                MinAmount = item.MinAmount,
            };
            if (item.ApproverType == ApproverType.Users)
            {
                foreach (var userId in item.UserIds!.Distinct())
                {
                    step.Users.Add(new ApprovalFlowStepUser { UserId = userId });
                }
            }

            flow.Steps.Add(step);
        }

        // Yalnızca adımlar değişse de akışın güncellenme bilgisi ilerler.
        if (db.Entry(flow).State == EntityState.Unchanged)
        {
            db.Entry(flow).State = EntityState.Modified;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(documentType, cancellationToken);
    }
}

/// <summary>Kullanıcının kararını bekleyen siparişler (liste süzgeci ve ana sayfa görevi için).</summary>
public static class SalesOrderApprovalQueries
{
    /// <param name="canApproveWithoutFlow">Akışsız gönderilmiş siparişleri onaylayabilir (müşteri siparişi onaylama yetkisi).</param>
    public static IQueryable<SalesOrder> AwaitingDecisionOf(
        this IQueryable<SalesOrder> orders, IKonfeksiyonDbContext db, int userId, IReadOnlyCollection<int> roleIds, bool canApproveWithoutFlow)
    {
        var token = $",{userId},";
        return orders.Where(o => o.Status == SalesOrderStatus.Submitted
            && (db.SalesOrderApprovalSteps.Any(s => s.SalesOrderId == o.Id && s.Round == o.ApprovalRound
                    && s.Status == ApprovalStepStatus.Pending
                    && ((s.ApproverType == ApproverType.Role && s.RoleId != null && roleIds.Contains(s.RoleId.Value))
                        || (s.ApproverType == ApproverType.Users && s.ApproverUserIds!.Contains(token))))
                || (canApproveWithoutFlow
                    && !db.SalesOrderApprovalSteps.Any(s => s.SalesOrderId == o.Id && s.Round == o.ApprovalRound))));
    }
}
