using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.Shipping;

public interface IAssortmentTemplateService
{
    Task<PagedResult<AssortmentTemplateListItemDto>> GetListAsync(AssortmentTemplateQuery query, CancellationToken cancellationToken);
    Task<Result<AssortmentTemplateDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<AssortmentTemplateDto>> CreateAsync(AssortmentTemplateRequest request, CancellationToken cancellationToken);
    Task<Result<AssortmentTemplateDto>> UpdateAsync(int id, AssortmentTemplateRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Koli dağıtımında seçilebilecek aktif şablonlar: müşteri verilirse o müşterinin şablonları ve genel şablonlar
    /// (müşteriye özel olanlar önce).
    /// </summary>
    Task<IReadOnlyList<AssortmentTemplateOptionDto>> GetOptionsAsync(int? customerId, CancellationToken cancellationToken);
}

/// <summary>Asorti şablonları: kolinin beden dağılımı. Koliler tek renklidir.</summary>
public sealed class AssortmentTemplateService(IKonfeksiyonDbContext db) : IAssortmentTemplateService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Asorti şablonu bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<AssortmentTemplateListItemDto>> GetListAsync(
        AssortmentTemplateQuery query, CancellationToken cancellationToken)
    {
        var templates = db.AssortmentTemplates.AsNoTracking();
        if (NullIfBlank(query.Search) is { } search)
        {
            templates = templates.Where(t => t.Code.Contains(search) || t.Name.Contains(search)
                || (t.Customer != null && t.Customer.Name.Contains(search)));
        }

        if (query.CustomerId is { } customerId)
        {
            templates = templates.Where(t => t.CustomerId == customerId);
        }

        return templates
            .OrderBy(t => t.Code)
            .Select(t => new AssortmentTemplateListItemDto(
                t.Id,
                t.Code,
                t.Name,
                t.CustomerId,
                t.Customer != null ? t.Customer.Name : null,
                t.Lines.Count,
                t.Lines.Sum(l => (int?)l.Ratio) ?? 0,
                t.IsActive))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<AssortmentTemplateDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        // Silinmiş müşteri de adıyla görünür.
        var template = await db.AssortmentTemplates.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new
            {
                t.Id, t.Code, t.Name, t.CustomerId, t.Notes, t.IsActive, t.CreatedAt, t.CreatedBy, t.UpdatedAt, t.UpdatedBy,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (template is null)
        {
            return NotFound;
        }

        var customer = template.CustomerId is { } customerId
            ? await db.Customers.AsNoTracking().IgnoreQueryFilters()
                .Where(c => c.Id == customerId)
                .Select(c => new { c.CustomerCode, c.Name })
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        // Sonradan silinen beden de adıyla görünür.
        var lines = await db.AssortmentTemplateLines.AsNoTracking().IgnoreQueryFilters()
            .Where(l => l.AssortmentTemplateId == id)
            .OrderBy(l => l.Size.SortOrder).ThenBy(l => l.Size.SizeCode)
            .Select(l => new AssortmentTemplateLineDto(l.Id, l.SizeId, l.Size.SizeCode, l.Size.Name, l.Size.SortOrder, l.Ratio))
            .ToListAsync(cancellationToken);

        return new AssortmentTemplateDto(
            template.Id, template.Code, template.Name, template.CustomerId, customer?.CustomerCode, customer?.Name,
            template.Notes, template.IsActive, lines.Sum(l => l.Ratio), template.CreatedAt, template.CreatedBy,
            template.UpdatedAt, template.UpdatedBy, lines);
    }

    public async Task<Result<AssortmentTemplateDto>> CreateAsync(AssortmentTemplateRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var template = new AssortmentTemplate();
        Apply(request, template);
        foreach (var line in request.Lines)
        {
            template.Lines.Add(new AssortmentTemplateLine { SizeId = line.SizeId, Ratio = line.Ratio });
        }

        db.AssortmentTemplates.Add(template);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(template.Id, cancellationToken);
    }

    public async Task<Result<AssortmentTemplateDto>> UpdateAsync(
        int id, AssortmentTemplateRequest request, CancellationToken cancellationToken)
    {
        var template = await db.AssortmentTemplates.Include(t => t.Lines).SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (template is null)
        {
            return NotFound;
        }

        if (await ValidateAsync(request, template, cancellationToken) is { } error)
        {
            return error;
        }

        Apply(request, template);

        // Satırlar istekle eşitlenir: kalan güncellenir, eksilen silinir, yeni gelen eklenir. Daha önce dağıtılmış
        // koliler etkilenmez; şablon yalnızca dağıtım anında kullanılır.
        var wanted = request.Lines.ToDictionary(l => l.SizeId, l => l.Ratio);
        foreach (var line in template.Lines.ToList())
        {
            if (wanted.Remove(line.SizeId, out var ratio))
            {
                line.Ratio = ratio;
            }
            else
            {
                db.AssortmentTemplateLines.Remove(line);
            }
        }

        foreach (var (sizeId, ratio) in wanted)
        {
            db.AssortmentTemplateLines.Add(new AssortmentTemplateLine { AssortmentTemplateId = template.Id, SizeId = sizeId, Ratio = ratio });
        }

        // Yalnızca satırlar değişse de şablonun güncellenme bilgisi ilerler.
        db.Entry(template).State = EntityState.Modified;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var template = await db.AssortmentTemplates.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (template is null)
        {
            return NotFound;
        }

        // Soft delete: şablonla doldurulmuş koliler şablon koduyla görünmeye devam eder.
        db.AssortmentTemplates.Remove(template);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<AssortmentTemplateOptionDto>> GetOptionsAsync(int? customerId, CancellationToken cancellationToken)
    {
        var templates = await db.AssortmentTemplates.AsNoTracking()
            .Where(t => t.IsActive && (t.CustomerId == null || t.CustomerId == customerId))
            .OrderBy(t => t.CustomerId == null).ThenBy(t => t.Code)
            .Select(t => new
            {
                t.Id,
                t.Code,
                t.Name,
                CustomerName = t.Customer != null ? t.Customer.Name : null,
                Lines = t.Lines
                    .OrderBy(l => l.Size.SortOrder).ThenBy(l => l.Size.SizeCode)
                    .Select(l => new { l.Size.SizeCode, l.Ratio })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return templates
            .Select(t => new AssortmentTemplateOptionDto(
                t.Id, t.Code, t.Name, t.CustomerName, t.Lines.Sum(l => l.Ratio),
                string.Join(" ", t.Lines.Select(l => $"{l.SizeCode}:{l.Ratio}"))))
            .ToList();
    }

    private async Task<Error?> ValidateAsync(
        AssortmentTemplateRequest request, AssortmentTemplate? existing, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.Code);
        if (await db.AssortmentTemplates.AnyAsync(
                t => t.Code == code && t.Id != (existing == null ? 0 : existing.Id), cancellationToken))
        {
            var message = $"'{code}' kodlu bir asorti şablonu zaten var.";
            return new Error(
                ErrorCodes.CodeAlreadyExists, message, ErrorType.Conflict, new Dictionary<string, string[]> { ["code"] = [message] });
        }

        var sizeIds = request.Lines.Select(l => l.SizeId).Distinct().ToList();
        return await ReferenceValidation.RequireSelectableAsync(
                db.Customers, request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Sizes, sizeIds, existing?.Lines.Select(l => l.SizeId).ToHashSet() ?? [], "lines", "Beden", cancellationToken);
    }

    private static void Apply(AssortmentTemplateRequest request, AssortmentTemplate template)
    {
        template.Code = NormalizeCode(request.Code);
        template.Name = request.Name.Trim();
        template.CustomerId = request.CustomerId;
        template.Notes = NullIfBlank(request.Notes);
        template.IsActive = request.IsActive;
    }
}
