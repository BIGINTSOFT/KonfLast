using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Common;
using ERP.Core.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Lab;

public sealed class BoyahaneRecipeQuery : PageRequest
{
    /// <summary>Talep no, müşteri, kart ya da renk.</summary>
    public string? Search { get; set; }
}

public sealed record BoyahaneRecipeItemDto(string Code, string Name, DyeChemicalKind Kind, decimal Amount);

/// <param name="ReusedFromRequestNo">Laboratuvar atlandıysa reçetenin geldiği talep.</param>
/// <param name="ColorCardCode">Sipariş satırında renk kartı seçildiyse kartın kodu.</param>
public sealed record BoyahaneRecipeDto(
    int TrialId,
    int RequestId,
    string RequestNo,
    string TrialCode,
    string OrderNo,
    string? CustomerName,
    string? CardCode,
    string? CardName,
    string? ColorCode,
    string? ColorName,
    DateOnly? DecisionOn,
    string? ReusedFromRequestNo,
    IReadOnlyList<BoyahaneRecipeItemDto> Items,
    string? ColorCardCode);

public interface IBoyahaneRecipeService
{
    Task<PagedResult<BoyahaneRecipeDto>> GetListAsync(BoyahaneRecipeQuery query, CancellationToken cancellationToken);
}

/// <summary>Reçete, müşterinin kabul ettiği lab denemesidir; bu liste onları tek yerde gösterir, ayrı kayıt tutmaz.</summary>
public sealed class BoyahaneRecipeService(IAppDbContext db) : IBoyahaneRecipeService
{
    public Task<PagedResult<BoyahaneRecipeDto>> GetListAsync(BoyahaneRecipeQuery query, CancellationToken cancellationToken)
    {
        var trials = db.Set<BoyahaneLabTrial>().AsNoTracking().Where(t => t.Decision == LabTrialDecision.Accepted);
        if (NullIfBlank(query.Search) is { } term)
        {
            trials = trials.Where(t => t.Request.RequestNo.Contains(term)
                || (t.Request.OrderLine.Order.Customer != null && t.Request.OrderLine.Order.Customer.Name.Contains(term))
                || (t.Request.OrderLine.Fabric != null && t.Request.OrderLine.Fabric.Name.Contains(term))
                || (t.Request.OrderLine.Yarn != null && t.Request.OrderLine.Yarn.Name.Contains(term))
                || (t.Request.OrderLine.ColorName != null && t.Request.OrderLine.ColorName.Contains(term))
                || (t.Request.OrderLine.ColorCode != null && t.Request.OrderLine.ColorCode.Contains(term))
                || (t.Request.OrderLine.ColorCard != null && t.Request.OrderLine.ColorCard.Code.Contains(term)));
        }

        return trials
            .OrderByDescending(t => t.Id)
            .Select(t => new BoyahaneRecipeDto(
                t.Id,
                t.RequestId,
                t.Request.RequestNo,
                t.TrialCode,
                t.Request.OrderLine.Order.OrderNo,
                t.Request.OrderLine.Order.Customer != null ? t.Request.OrderLine.Order.Customer.Name : null,
                t.Request.OrderLine.Fabric != null ? t.Request.OrderLine.Fabric.FabricCode : t.Request.OrderLine.Yarn != null ? t.Request.OrderLine.Yarn.YarnCode : null,
                t.Request.OrderLine.Fabric != null ? t.Request.OrderLine.Fabric.Name : t.Request.OrderLine.Yarn != null ? t.Request.OrderLine.Yarn.Name : null,
                t.Request.OrderLine.ColorCode,
                t.Request.OrderLine.ColorName,
                t.DecisionOn,
                t.ReusedFromTrial != null ? t.ReusedFromTrial.Request.RequestNo : null,
                t.Items.OrderBy(i => i.DyeChemical.Kind).ThenBy(i => i.Id)
                    .Select(i => new BoyahaneRecipeItemDto(i.DyeChemical.Code, i.DyeChemical.Name, i.DyeChemical.Kind, i.Amount))
                    .ToList(),
                t.Request.OrderLine.ColorCard != null ? t.Request.OrderLine.ColorCard.Code : null))
            .ToPagedResultAsync(query, cancellationToken);
    }
}
