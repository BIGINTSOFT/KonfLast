using System.Text.RegularExpressions;
using ERP.Dokuma.Domain.Entities;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Customers;

public sealed record DokumaCustomerAddressDto(
    int Id,
    string Title,
    string AddressLine,
    string District,
    string City,
    string? PostalCode,
    string CountryCode,
    bool IsDefaultInvoice,
    bool IsDefaultDelivery);

public sealed record DokumaCustomerAddressesDto(int CustomerId, IReadOnlyList<DokumaCustomerAddressDto> Addresses);

/// <param name="Id">Var olan adres güncelleniyorsa adresin Id'si; yeni adreste boş.</param>
/// <param name="CountryCode">ISO 3166-1 alfa-2 ülke kodu; boşsa TR.</param>
public sealed record DokumaCustomerAddressRequest(
    int? Id,
    string Title,
    string AddressLine,
    string District,
    string City,
    string? PostalCode = null,
    string? CountryCode = null,
    bool IsDefaultInvoice = false,
    bool IsDefaultDelivery = false);

/// <summary>Müşterinin adreslerinin tamamı; gönderilmeyen adresler silinir.</summary>
public sealed record SaveDokumaCustomerAddressesRequest(IReadOnlyList<DokumaCustomerAddressRequest> Addresses);

public sealed partial class SaveDokumaCustomerAddressesRequestValidator : AbstractValidator<SaveDokumaCustomerAddressesRequest>
{
    public const int MaxAddresses = 20;

    public SaveDokumaCustomerAddressesRequestValidator()
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
            });
    }

    private static IEnumerable<string> Problems(DokumaCustomerAddressRequest address)
    {
        if (string.IsNullOrWhiteSpace(address.Title) || address.Title.Trim().Length > 100)
        {
            yield return "Adres adı zorunludur (en fazla 100 karakter).";
        }

        if (string.IsNullOrWhiteSpace(address.AddressLine) || address.AddressLine.Trim().Length > 500)
        {
            yield return "Açık adres zorunludur (en fazla 500 karakter).";
        }

        if (string.IsNullOrWhiteSpace(address.District) || address.District.Trim().Length > 100)
        {
            yield return "İlçe zorunludur (en fazla 100 karakter).";
        }

        if (string.IsNullOrWhiteSpace(address.City) || address.City.Trim().Length > 100)
        {
            yield return "İl zorunludur (en fazla 100 karakter).";
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
    }

    internal static string CountryOf(DokumaCustomerAddressRequest address) =>
        NullIfBlank(address.CountryCode)?.ToUpperInvariant() ?? "TR";

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex CountryPattern();

    [GeneratedRegex(@"^\d{5}$")]
    private static partial Regex TurkishPostalCode();
}

public sealed record DokumaCustomerContactDto(int Id, string Name, string? Title, string? Phone, string? Email);

public sealed record DokumaCustomerContactsDto(int CustomerId, IReadOnlyList<DokumaCustomerContactDto> Contacts);

/// <param name="Id">Var olan yetkili güncelleniyorsa Id'si; yenisinde boş.</param>
public sealed record DokumaCustomerContactRequest(int? Id, string Name, string? Title = null, string? Phone = null, string? Email = null);

/// <summary>Müşterinin yetkililerinin tamamı; gönderilmeyenler silinir.</summary>
public sealed record SaveDokumaCustomerContactsRequest(IReadOnlyList<DokumaCustomerContactRequest> Contacts);

public sealed class SaveDokumaCustomerContactsRequestValidator : AbstractValidator<SaveDokumaCustomerContactsRequest>
{
    public const int MaxContacts = 50;

    public SaveDokumaCustomerContactsRequestValidator()
    {
        RuleFor(x => x.Contacts)
            .NotNull().WithMessage("Yetkili listesi zorunludur.")
            .Must(list => list.Count <= MaxContacts).WithMessage($"En fazla {MaxContacts} yetkili girilebilir.");

        RuleForEach(x => x.Contacts).ChildRules(contact =>
        {
            contact.RuleFor(c => c.Name)
                .NotEmpty().WithMessage("Yetkili adı zorunludur.")
                .MaximumLength(100).WithMessage("Yetkili adı en fazla 100 karakter olabilir.");
            contact.RuleFor(c => c.Title).MaximumLength(100).WithMessage("Görev en fazla 100 karakter olabilir.");
            contact.RuleFor(c => c.Phone).MaximumLength(30).WithMessage("Telefon en fazla 30 karakter olabilir.");
            contact.RuleFor(c => c.Email)
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
                .MaximumLength(256)
                .When(c => !string.IsNullOrWhiteSpace(c.Email));
        });
    }
}

public interface IDokumaCustomerDetailService
{
    Task<Result<DokumaCustomerAddressesDto>> GetAddressesAsync(int customerId, CancellationToken cancellationToken);
    Task<Result<DokumaCustomerAddressesDto>> SaveAddressesAsync(int customerId, SaveDokumaCustomerAddressesRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaCustomerContactsDto>> GetContactsAsync(int customerId, CancellationToken cancellationToken);
    Task<Result<DokumaCustomerContactsDto>> SaveContactsAsync(int customerId, SaveDokumaCustomerContactsRequest request, CancellationToken cancellationToken);
}

/// <summary>Müşterinin adresleri ve yetkilileri: liste bütün olarak kaydedilir.</summary>
public sealed class DokumaCustomerDetailService(IAppDbContext db) : IDokumaCustomerDetailService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Müşteri bulunamadı.", ErrorType.NotFound);

    private Task<bool> CustomerExistsAsync(int customerId, CancellationToken cancellationToken) =>
        db.Set<DokumaCustomer>().AnyAsync(c => c.Id == customerId, cancellationToken);

    public async Task<Result<DokumaCustomerAddressesDto>> GetAddressesAsync(int customerId, CancellationToken cancellationToken)
    {
        if (!await CustomerExistsAsync(customerId, cancellationToken))
        {
            return NotFound;
        }

        // Varsayılan fatura adresi önce, sonra varsayılan teslimat, sonra ada göre.
        var addresses = await db.Set<DokumaCustomerAddress>().AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsDefaultInvoice).ThenByDescending(a => a.IsDefaultDelivery).ThenBy(a => a.Title)
            .Select(a => new DokumaCustomerAddressDto(
                a.Id, a.Title, a.AddressLine, a.District, a.City, a.PostalCode, a.CountryCode, a.IsDefaultInvoice, a.IsDefaultDelivery))
            .ToListAsync(cancellationToken);

        return new DokumaCustomerAddressesDto(customerId, addresses);
    }

    public async Task<Result<DokumaCustomerAddressesDto>> SaveAddressesAsync(
        int customerId, SaveDokumaCustomerAddressesRequest request, CancellationToken cancellationToken)
    {
        if (!await CustomerExistsAsync(customerId, cancellationToken))
        {
            return NotFound;
        }

        var set = db.Set<DokumaCustomerAddress>();
        var existing = await set.Where(a => a.CustomerId == customerId).ToListAsync(cancellationToken);
        var foreign = request.Addresses.FirstOrDefault(a => a.Id is { } id && existing.All(e => e.Id != id));
        if (foreign is not null)
        {
            return ReferenceValidation.InvalidReference("addresses", $"\"{foreign.Title}\" adresi bu müşteriye ait değil.");
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

        set.RemoveRange(existing.Where(e => request.Addresses.All(a => a.Id != e.Id)));
        await db.SaveChangesAsync(cancellationToken);

        foreach (var item in request.Addresses)
        {
            var address = item.Id is { } id ? existing.Single(e => e.Id == id) : new DokumaCustomerAddress { CustomerId = customerId };
            address.Title = item.Title.Trim();
            address.AddressLine = item.AddressLine.Trim();
            address.District = item.District.Trim();
            address.City = item.City.Trim();
            address.PostalCode = NullIfBlank(item.PostalCode);
            address.CountryCode = SaveDokumaCustomerAddressesRequestValidator.CountryOf(item);
            address.IsDefaultInvoice = ReferenceEquals(item, defaultInvoice);
            address.IsDefaultDelivery = ReferenceEquals(item, defaultDelivery);
            if (item.Id is null)
            {
                set.Add(address);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAddressesAsync(customerId, cancellationToken);
    }

    public async Task<Result<DokumaCustomerContactsDto>> GetContactsAsync(int customerId, CancellationToken cancellationToken)
    {
        if (!await CustomerExistsAsync(customerId, cancellationToken))
        {
            return NotFound;
        }

        var contacts = await db.Set<DokumaCustomerContact>().AsNoTracking()
            .Where(c => c.CustomerId == customerId)
            .OrderBy(c => c.Name)
            .Select(c => new DokumaCustomerContactDto(c.Id, c.Name, c.Title, c.Phone, c.Email))
            .ToListAsync(cancellationToken);

        return new DokumaCustomerContactsDto(customerId, contacts);
    }

    public async Task<Result<DokumaCustomerContactsDto>> SaveContactsAsync(
        int customerId, SaveDokumaCustomerContactsRequest request, CancellationToken cancellationToken)
    {
        if (!await CustomerExistsAsync(customerId, cancellationToken))
        {
            return NotFound;
        }

        var set = db.Set<DokumaCustomerContact>();
        var existing = await set.Where(c => c.CustomerId == customerId).ToListAsync(cancellationToken);
        var foreign = request.Contacts.FirstOrDefault(c => c.Id is { } id && existing.All(e => e.Id != id));
        if (foreign is not null)
        {
            return ReferenceValidation.InvalidReference("contacts", $"\"{foreign.Name}\" bu müşterinin yetkilisi değil.");
        }

        set.RemoveRange(existing.Where(e => request.Contacts.All(c => c.Id != e.Id)));

        foreach (var item in request.Contacts)
        {
            var contact = item.Id is { } id ? existing.Single(e => e.Id == id) : new DokumaCustomerContact { CustomerId = customerId };
            contact.Name = item.Name.Trim();
            contact.Title = NullIfBlank(item.Title);
            contact.Phone = NullIfBlank(item.Phone);
            contact.Email = NullIfBlank(item.Email);
            if (item.Id is null)
            {
                set.Add(contact);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetContactsAsync(customerId, cancellationToken);
    }
}
