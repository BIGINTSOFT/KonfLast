using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.MasterData;

public sealed record CustomerBuyerDto(
    int Id, string Code, string Name, string? ContactName, string? Email, string? Phone, bool IsActive);

public sealed record CustomerBuyersDto(int CustomerId, string CustomerCode, string CustomerName, IReadOnlyList<CustomerBuyerDto> Buyers);

/// <param name="Id">Var olan buyer güncelleniyorsa Id'si; yeni buyer'da boş.</param>
public sealed record CustomerBuyerRequest(
    int? Id, string Code, string Name, string? ContactName = null, string? Email = null, string? Phone = null, bool IsActive = true);

/// <summary>Müşterinin buyer'larının tamamı; gönderilmeyenler silinir (siparişte kullanılmışsa silinemez, pasif yapılır).</summary>
public sealed record SaveCustomerBuyersRequest(IReadOnlyList<CustomerBuyerRequest> Buyers);

public sealed class SaveCustomerBuyersRequestValidator : AbstractValidator<SaveCustomerBuyersRequest>
{
    public const int MaxBuyers = 50;

    public SaveCustomerBuyersRequestValidator()
    {
        // Hatalar tek alanda ("buyers") toplanır; ileti hangi satırın hatalı olduğunu söyler.
        RuleFor(x => x.Buyers)
            .NotNull().WithMessage("Buyer listesi zorunludur.")
            .Must(list => list.Count <= MaxBuyers).WithMessage($"En fazla {MaxBuyers} buyer girilebilir.")
            .Custom((list, context) =>
            {
                if (list is null)
                {
                    return;
                }

                for (var i = 0; i < list.Count; i++)
                {
                    foreach (var problem in Problems(list[i]))
                    {
                        context.AddFailure($"{i + 1}. buyer: {problem}");
                    }
                }

                var duplicate = list
                    .Where(b => !string.IsNullOrWhiteSpace(b.Code))
                    .GroupBy(b => NormalizeCode(b.Code))
                    .FirstOrDefault(g => g.Count() > 1);
                if (duplicate is not null)
                {
                    context.AddFailure($"\"{duplicate.Key}\" kodu birden fazla buyer'da kullanılmış.");
                }
            });
    }

    private static IEnumerable<string> Problems(CustomerBuyerRequest buyer)
    {
        if (string.IsNullOrWhiteSpace(buyer.Code))
        {
            yield return "Kod zorunludur.";
        }
        else if (buyer.Code.Trim().Length > 30)
        {
            yield return "Kod en fazla 30 karakter olabilir.";
        }

        if (string.IsNullOrWhiteSpace(buyer.Name))
        {
            yield return "Ad zorunludur.";
        }
        else if (buyer.Name.Trim().Length > 200)
        {
            yield return "Ad en fazla 200 karakter olabilir.";
        }

        if (buyer.ContactName?.Trim().Length > 100 || buyer.Phone?.Trim().Length > 30)
        {
            yield return "İlgili kişi en fazla 100, telefon en fazla 30 karakter olabilir.";
        }

        var email = NullIfBlank(buyer.Email);
        if (email is not null && (email.Length > 256 || !email.Contains('@')))
        {
            yield return "Geçerli bir e-posta adresi girin.";
        }
    }
}

public interface ICustomerBuyerService
{
    Task<Result<CustomerBuyersDto>> GetAsync(int customerId, CancellationToken cancellationToken);
    Task<Result<CustomerBuyersDto>> SaveAsync(int customerId, SaveCustomerBuyersRequest request, CancellationToken cancellationToken);
}

public sealed class CustomerBuyerService(IKonfeksiyonDbContext db, IReferenceUsage usage) : ICustomerBuyerService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Müşteri bulunamadı.", ErrorType.NotFound);

    public async Task<Result<CustomerBuyersDto>> GetAsync(int customerId, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new { c.Id, c.CustomerCode, c.Name })
            .SingleOrDefaultAsync(cancellationToken);
        if (customer is null)
        {
            return NotFound;
        }

        var buyers = await db.CustomerBuyers.AsNoTracking()
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.IsActive).ThenBy(b => b.Code)
            .Select(b => new CustomerBuyerDto(b.Id, b.Code, b.Name, b.ContactName, b.Email, b.Phone, b.IsActive))
            .ToListAsync(cancellationToken);

        return new CustomerBuyersDto(customer.Id, customer.CustomerCode, customer.Name, buyers);
    }

    public async Task<Result<CustomerBuyersDto>> SaveAsync(
        int customerId, SaveCustomerBuyersRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Customers.AnyAsync(c => c.Id == customerId, cancellationToken))
        {
            return NotFound;
        }

        var existing = await db.CustomerBuyers.Where(b => b.CustomerId == customerId).ToListAsync(cancellationToken);
        var foreign = request.Buyers.FirstOrDefault(b => b.Id is { } id && existing.All(e => e.Id != id));
        if (foreign is not null)
        {
            return ReferenceValidation.InvalidReference("buyers", $"\"{foreign.Name}\" bu müşterinin buyer'ı değil.");
        }

        // Siparişte kullanılmış buyer silinmez; geçmiş siparişler kimin adına verildiğini göstermeye devam eder.
        var removed = existing.Where(e => request.Buyers.All(b => b.Id != e.Id)).ToList();
        var removedIds = removed.Select(b => b.Id).ToList();
        var used = new List<int>();
        foreach (var removedId in removedIds)
        {
            if (await usage.FindAsync<CustomerBuyer>(removedId, cancellationToken) is not null)
            {
                used.Add(removedId);
            }
        }

        if (used.Count > 0)
        {
            var name = removed.First(b => used.Contains(b.Id)).Name;
            return ReferenceValidation.InvalidReference(
                "buyers", $"\"{name}\" siparişlerde kullanıldığı için silinemez; pasif yapın.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Kod tekil dizinine takılmasın diye silinenler önce kaldırılır.
        db.CustomerBuyers.RemoveRange(removed);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var item in request.Buyers)
        {
            var buyer = item.Id is { } id ? existing.Single(e => e.Id == id) : new CustomerBuyer { CustomerId = customerId };
            buyer.Code = NormalizeCode(item.Code);
            buyer.Name = item.Name.Trim();
            buyer.ContactName = NullIfBlank(item.ContactName);
            buyer.Email = NullIfBlank(item.Email);
            buyer.Phone = NullIfBlank(item.Phone);
            buyer.IsActive = item.IsActive;
            if (item.Id is null)
            {
                db.CustomerBuyers.Add(buyer);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(customerId, cancellationToken);
    }
}
