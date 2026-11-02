using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using System.Text.RegularExpressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.MasterData;

public sealed record CustomerAddressDto(
    int Id,
    string Title,
    string AddressLine,
    string District,
    string City,
    string? PostalCode,
    string CountryCode,
    string? ContactName,
    string? Phone,
    bool IsDefaultInvoice,
    bool IsDefaultDelivery);

public sealed record CustomerAddressesDto(int CustomerId, string CustomerCode, string CustomerName, IReadOnlyList<CustomerAddressDto> Addresses);

/// <param name="Id">Var olan adres güncelleniyorsa adresin Id'si; yeni adreste boş.</param>
/// <param name="CountryCode">ISO 3166-1 alfa-2 ülke kodu; boşsa TR.</param>
public sealed record CustomerAddressRequest(
    int? Id,
    string Title,
    string AddressLine,
    string District,
    string City,
    string? PostalCode = null,
    string? CountryCode = null,
    string? ContactName = null,
    string? Phone = null,
    bool IsDefaultInvoice = false,
    bool IsDefaultDelivery = false);

/// <summary>Müşterinin adreslerinin tamamı; gönderilmeyen adresler silinir.</summary>
public sealed record SaveCustomerAddressesRequest(IReadOnlyList<CustomerAddressRequest> Addresses);

public sealed partial class SaveCustomerAddressesRequestValidator : AbstractValidator<SaveCustomerAddressesRequest>
{
    public const int MaxAddresses = 20;

    public SaveCustomerAddressesRequestValidator()
    {
        // Hatalar tek alanda ("addresses") toplanır; ileti hangi satırın hatalı olduğunu söyler.
        RuleFor(x => x.Addresses)
            .NotNull().WithMessage("Adres listesi zorunludur.")
            .Must(list => list.Count <= MaxAddresses).WithMessage($"En fazla {MaxAddresses} adres girilebilir.")
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
                        context.AddFailure($"{i + 1}. adres: {problem}");
                    }
                }

                if (list.Count(a => a.IsDefaultInvoice) > 1)
                {
                    context.AddFailure("Yalnızca bir adres varsayılan fatura adresi olabilir.");
                }

                if (list.Count(a => a.IsDefaultDelivery) > 1)
                {
                    context.AddFailure("Yalnızca bir adres varsayılan teslimat adresi olabilir.");
                }

                var duplicate = list
                    .Where(a => !string.IsNullOrWhiteSpace(a.Title))
                    .GroupBy(a => a.Title.Trim().ToUpperInvariant())
                    .FirstOrDefault(g => g.Count() > 1);
                if (duplicate is not null)
                {
                    context.AddFailure($"\"{duplicate.First().Title.Trim()}\" adı birden fazla adreste kullanılmış.");
                }
            });
    }

    private static IEnumerable<string> Problems(CustomerAddressRequest address)
    {
        if (string.IsNullOrWhiteSpace(address.Title))
        {
            yield return "Adres adı zorunludur.";
        }
        else if (address.Title.Trim().Length > 100)
        {
            yield return "Adres adı en fazla 100 karakter olabilir.";
        }

        if (string.IsNullOrWhiteSpace(address.AddressLine))
        {
            yield return "Açık adres zorunludur.";
        }
        else if (address.AddressLine.Trim().Length > 500)
        {
            yield return "Açık adres en fazla 500 karakter olabilir.";
        }

        if (string.IsNullOrWhiteSpace(address.District))
        {
            yield return "İlçe zorunludur.";
        }

        if (string.IsNullOrWhiteSpace(address.City))
        {
            yield return "İl zorunludur.";
        }

        if (address.District?.Trim().Length > 100 || address.City?.Trim().Length > 100)
        {
            yield return "İl ve ilçe en fazla 100 karakter olabilir.";
        }

        var country = CountryOf(address);
        if (!CountryPattern().IsMatch(country))
        {
            yield return "Ülke kodu iki harf olmalıdır (örn. TR, DE).";
        }

        var postalCode = NullIfBlank(address.PostalCode);
        if (postalCode is not null && (country == "TR" ? !TurkishPostalCode().IsMatch(postalCode) : postalCode.Length > 10))
        {
            yield return country == "TR" ? "Posta kodu 5 haneli olmalıdır." : "Posta kodu en fazla 10 karakter olabilir.";
        }

        if (address.ContactName?.Trim().Length > 100 || address.Phone?.Trim().Length > 30)
        {
            yield return "İlgili kişi en fazla 100, telefon en fazla 30 karakter olabilir.";
        }
    }

    internal static string CountryOf(CustomerAddressRequest address) =>
        NullIfBlank(address.CountryCode)?.ToUpperInvariant() ?? "TR";

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex CountryPattern();

    [GeneratedRegex(@"^\d{5}$")]
    private static partial Regex TurkishPostalCode();
}

public interface ICustomerAddressService
{
    Task<Result<CustomerAddressesDto>> GetAsync(int customerId, CancellationToken cancellationToken);
    Task<Result<CustomerAddressesDto>> SaveAsync(int customerId, SaveCustomerAddressesRequest request, CancellationToken cancellationToken);
}

public sealed class CustomerAddressService(IKonfeksiyonDbContext db, IReferenceUsage usage) : ICustomerAddressService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Müşteri bulunamadı.", ErrorType.NotFound);

    public async Task<Result<CustomerAddressesDto>> GetAsync(int customerId, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new { c.Id, c.CustomerCode, c.Name })
            .SingleOrDefaultAsync(cancellationToken);
        if (customer is null)
        {
            return NotFound;
        }

        // Varsayılan fatura adresi önce, sonra varsayılan teslimat, sonra ada göre.
        var addresses = await db.CustomerAddresses.AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsDefaultInvoice).ThenByDescending(a => a.IsDefaultDelivery).ThenBy(a => a.Title)
            .Select(a => new CustomerAddressDto(
                a.Id, a.Title, a.AddressLine, a.District, a.City, a.PostalCode, a.CountryCode, a.ContactName, a.Phone,
                a.IsDefaultInvoice, a.IsDefaultDelivery))
            .ToListAsync(cancellationToken);

        return new CustomerAddressesDto(customer.Id, customer.CustomerCode, customer.Name, addresses);
    }

    public async Task<Result<CustomerAddressesDto>> SaveAsync(
        int customerId, SaveCustomerAddressesRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Customers.AnyAsync(c => c.Id == customerId, cancellationToken))
        {
            return NotFound;
        }

        var existing = await db.CustomerAddresses.Where(a => a.CustomerId == customerId).ToListAsync(cancellationToken);
        var foreign = request.Addresses.FirstOrDefault(a => a.Id is { } id && existing.All(e => e.Id != id));
        if (foreign is not null)
        {
            return ReferenceValidation.InvalidReference("addresses", $"\"{foreign.Title}\" adresi bu müşteriye ait değil.");
        }

        // Siparişin teslim adresi olarak kullanılmış adres silinmez.
        var removedIds = existing.Where(e => request.Addresses.All(a => a.Id != e.Id)).Select(e => e.Id).ToList();
        foreach (var removedId in removedIds)
        {
            if (await usage.FindAsync<CustomerAddress>(removedId, cancellationToken) is { } usedIn)
            {
                var title = existing.Single(e => e.Id == removedId).Title;
                return ReferenceValidation.InvalidReference(
                    "addresses", $"\"{title}\" adresi {usedIn} kullanıldığı için silinemez.");
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Varsayılan işaretlenmemişse ilk adres varsayılan fatura / teslimat adresi olur.
        var defaultInvoice = request.Addresses.FirstOrDefault(a => a.IsDefaultInvoice) ?? request.Addresses.FirstOrDefault();
        var defaultDelivery = request.Addresses.FirstOrDefault(a => a.IsDefaultDelivery) ?? request.Addresses.FirstOrDefault();

        // Benzersiz varsayılan dizinleri, kaldırılan ya da değişen satırlarla çakışmasın diye önce bayraklar indirilir.
        foreach (var address in existing)
        {
            address.IsDefaultInvoice = false;
            address.IsDefaultDelivery = false;
        }

        db.CustomerAddresses.RemoveRange(existing.Where(e => request.Addresses.All(a => a.Id != e.Id)));
        await db.SaveChangesAsync(cancellationToken);

        foreach (var item in request.Addresses)
        {
            var address = item.Id is { } id ? existing.Single(e => e.Id == id) : new CustomerAddress { CustomerId = customerId };
            address.Title = item.Title.Trim();
            address.AddressLine = item.AddressLine.Trim();
            address.District = item.District.Trim();
            address.City = item.City.Trim();
            address.PostalCode = NullIfBlank(item.PostalCode);
            address.CountryCode = SaveCustomerAddressesRequestValidator.CountryOf(item);
            address.ContactName = NullIfBlank(item.ContactName);
            address.Phone = NullIfBlank(item.Phone);
            address.IsDefaultInvoice = ReferenceEquals(item, defaultInvoice);
            address.IsDefaultDelivery = ReferenceEquals(item, defaultDelivery);
            if (item.Id is null)
            {
                db.CustomerAddresses.Add(address);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(customerId, cancellationToken);
    }
}
