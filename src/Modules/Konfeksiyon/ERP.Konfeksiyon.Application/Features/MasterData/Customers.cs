using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Domain.Rules;
using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.MasterData;

/// <param name="AddressCount">Kayıtlı adres sayısı.</param>
/// <param name="InvoiceCity">Varsayılan fatura adresinin ili (listede gösterilir).</param>
public sealed record CustomerDto(
    int Id,
    string CustomerCode,
    string Name,
    string? TaxNumber,
    string? Phone,
    string? Email,
    bool IsActive,
    PartyType PartyType,
    string? FirstName,
    string? LastName,
    string? TaxOffice,
    string? MersisNo,
    string? TradeRegistryNo,
    bool IsEInvoiceUser,
    string? EInvoiceAlias,
    EInvoiceProfile EInvoiceProfile,
    bool IsEDespatchUser,
    string? EDespatchAlias,
    EArchiveDelivery EArchiveDelivery,
    string? EArchiveEmail,
    int AddressCount,
    string? InvoiceCity,
    string Currency,
    bool RequireUniqueCustomerOrderNo,
    int BuyerCount,
    int? PaymentTermDays,
    DocumentLanguage DocumentLanguage);

/// <param name="TaxNumber">VKN (tüzel, 10 hane) ya da TCKN (gerçek kişi, 11 hane); yurt dışı müşteride serbest.</param>
/// <param name="EInvoiceAlias">e-Fatura posta kutusu etiketi; e-Fatura mükellefinde zorunlu (urn:mail:…).</param>
/// <param name="EDespatchAlias">e-İrsaliye posta kutusu etiketi; e-İrsaliye mükellefinde zorunlu.</param>
/// <param name="EArchiveEmail">e-Arşiv faturanın gönderileceği e-posta; boşsa genel e-posta kullanılır.</param>
/// <param name="PaymentTermDays">Ödeme vadesi (gün).</param>
/// <param name="DocumentLanguage">Müşteriye basılan belgelerin dili.</param>
public sealed record CustomerRequest(
    string CustomerCode,
    string Name,
    string? TaxNumber,
    string? Phone,
    string? Email,
    bool IsActive = true,
    PartyType PartyType = PartyType.Company,
    string? FirstName = null,
    string? LastName = null,
    string? TaxOffice = null,
    string? MersisNo = null,
    string? TradeRegistryNo = null,
    bool IsEInvoiceUser = false,
    string? EInvoiceAlias = null,
    EInvoiceProfile EInvoiceProfile = EInvoiceProfile.Basic,
    bool IsEDespatchUser = false,
    string? EDespatchAlias = null,
    EArchiveDelivery EArchiveDelivery = EArchiveDelivery.Paper,
    string? EArchiveEmail = null,
    string? Currency = null,
    bool RequireUniqueCustomerOrderNo = false,
    int? PaymentTermDays = null,
    DocumentLanguage DocumentLanguage = DocumentLanguage.Turkish);

public sealed class CustomerRequestValidator : AbstractValidator<CustomerRequest>
{
    /// <summary>GİB posta kutusu etiketi: urn:mail:{ad}@{alan}.</summary>
    private const string AliasPattern = @"^\s*urn:mail:[^\s@]+@[^\s@]+\.[^\s@]+\s*$";

    public CustomerRequestValidator()
    {
        RuleFor(x => x.CustomerCode).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.PartyType).IsInEnum().WithMessage("Geçerli bir müşteri türü seçin.");
        RuleFor(x => x.Phone).OptionalText(30);
        RuleFor(x => x.TaxOffice).OptionalText(100);
        RuleFor(x => x.TradeRegistryNo).OptionalText(30);
        ValidEmail(x => x.Email);
        ValidEmail(x => x.EArchiveEmail);

        // Gerçek kişi e-belgede ad ve soyadla yer alır.
        When(x => x.PartyType == PartyType.Individual, () =>
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Gerçek kişi müşteride ad zorunludur.")
                .MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir.");
            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Gerçek kişi müşteride soyad zorunludur.")
                .MaximumLength(100).WithMessage("Soyad en fazla 100 karakter olabilir.");
        });

        RuleFor(x => x.TaxNumber)
            .MaximumLength(20).WithMessage("En fazla 20 karakter olabilir.")
            .Must((request, value) => IsValidTaxNumber(request.PartyType, value!.Trim()))
            .WithMessage(request => request.PartyType == PartyType.Individual
                ? "Geçerli bir T.C. kimlik numarası girin (11 hane)."
                : "Geçerli bir vergi kimlik numarası girin (10 hane).")
            .When(x => !string.IsNullOrWhiteSpace(x.TaxNumber) && TaxIdentity.IsDomestic(x.TaxNumber.Trim()));

        // e-Fatura ve e-İrsaliye yalnızca GİB'e kayıtlı (Türk vergi kimliği olan) alıcılara gönderilir.
        RuleFor(x => x.TaxNumber)
            .Must(value => !string.IsNullOrWhiteSpace(value) && TaxIdentity.IsDomestic(value.Trim()))
            .WithMessage("e-Fatura / e-İrsaliye mükellefinde VKN ya da TCKN zorunludur.")
            .When(x => x.IsEInvoiceUser || x.IsEDespatchUser);

        RuleFor(x => x.MersisNo)
            .Matches(@"^\s*\d{16}\s*$").WithMessage("MERSİS numarası 16 haneli olmalıdır.")
            .When(x => !string.IsNullOrWhiteSpace(x.MersisNo));

        RuleFor(x => x.EInvoiceAlias)
            .NotEmpty().WithMessage("e-Fatura mükellefinde posta kutusu etiketi zorunludur.")
            .Matches(AliasPattern).WithMessage("Etiket urn:mail:…@… biçiminde olmalıdır (örn. urn:mail:defaultpk@firma.com.tr).")
            .MaximumLength(256)
            .When(x => x.IsEInvoiceUser);
        RuleFor(x => x.EInvoiceProfile).IsInEnum().WithMessage("Geçerli bir e-Fatura senaryosu seçin.");

        RuleFor(x => x.EDespatchAlias)
            .NotEmpty().WithMessage("e-İrsaliye mükellefinde posta kutusu etiketi zorunludur.")
            .Matches(AliasPattern).WithMessage("Etiket urn:mail:…@… biçiminde olmalıdır (örn. urn:mail:irsaliyepk@firma.com.tr).")
            .MaximumLength(256)
            .When(x => x.IsEDespatchUser);

        RuleFor(x => x.Currency)
            .Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır (örn. TRY, EUR).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));

        RuleFor(x => x.EArchiveDelivery).IsInEnum().WithMessage("Geçerli bir teslim şekli seçin.");
        RuleFor(x => x.DocumentLanguage).IsInEnum().WithMessage("Geçerli bir belge dili seçin.");
        // e-Arşiv fatura elektronik teslim ediliyorsa gönderilecek bir e-posta olmalıdır.
        RuleFor(x => x.PaymentTermDays)
            .InclusiveBetween(0, 365).WithMessage("Vade 0 ile 365 gün arasında olmalıdır.")
            .When(x => x.PaymentTermDays is not null);

        RuleFor(x => x.EArchiveEmail)
            .Must((request, value) => !string.IsNullOrWhiteSpace(value) || !string.IsNullOrWhiteSpace(request.Email))
            .WithMessage("Elektronik teslim edilen e-Arşiv fatura için e-Arşiv ya da genel e-posta adresi girin.")
            .When(x => !x.IsEInvoiceUser && x.EArchiveDelivery == EArchiveDelivery.Electronic);
    }

    private static bool IsValidTaxNumber(PartyType partyType, string value) =>
        partyType == PartyType.Individual ? TaxIdentity.IsValidTckn(value) : TaxIdentity.IsValidVkn(value);

    private void ValidEmail(Expression<Func<CustomerRequest, string?>> field) =>
        RuleFor(field)
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(field.Compile()(x)));
}

public sealed class CustomerService(IKonfeksiyonDbContext db, IReferenceUsage usage) : KonfeksiyonMasterDataService<Customer, CustomerDto, CustomerRequest>(db)
{
    protected override DbSet<Customer> Set => Db.Customers;
    protected override string Label => "Müşteri";
    protected override string CodeField => "customerCode";

    protected override string CodeOf(CustomerRequest request) => request.CustomerCode;
    protected override Expression<Func<Customer, bool>> WithCode(string code) => e => e.CustomerCode == code;

    protected override Expression<Func<Customer, bool>> Matches(string term) =>
        e => e.CustomerCode.Contains(term) || e.Name.Contains(term) || (e.TaxNumber != null && e.TaxNumber.Contains(term));

    protected override IQueryable<Customer> Order(IQueryable<Customer> query) => query.OrderBy(e => e.CustomerCode);

    protected override Expression<Func<Customer, CustomerDto>> ToDto => e => new CustomerDto(
        e.Id, e.CustomerCode, e.Name, e.TaxNumber, e.Phone, e.Email, e.IsActive, e.PartyType, e.FirstName, e.LastName,
        e.TaxOffice, e.MersisNo, e.TradeRegistryNo, e.IsEInvoiceUser, e.EInvoiceAlias, e.EInvoiceProfile,
        e.IsEDespatchUser, e.EDespatchAlias, e.EArchiveDelivery, e.EArchiveEmail,
        e.Addresses.Count,
        e.Addresses.Where(a => a.IsDefaultInvoice).Select(a => a.City).FirstOrDefault(),
        e.Currency, e.RequireUniqueCustomerOrderNo, e.Buyers.Count, e.PaymentTermDays, e.DocumentLanguage);

    protected override Expression<Func<Customer, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.CustomerCode, e.Name);

    protected override void Apply(CustomerRequest request, Customer entity, string code)
    {
        var individual = request.PartyType == PartyType.Individual;
        entity.CustomerCode = code;
        entity.Name = request.Name.Trim();
        entity.PartyType = request.PartyType;
        // Ad / soyad yalnızca gerçek kişide anlamlıdır; tür değişince eski değer kalmaz.
        entity.FirstName = individual ? NullIfBlank(request.FirstName) : null;
        entity.LastName = individual ? NullIfBlank(request.LastName) : null;
        entity.TaxNumber = NullIfBlank(request.TaxNumber);
        entity.TaxOffice = NullIfBlank(request.TaxOffice);
        entity.MersisNo = NullIfBlank(request.MersisNo);
        entity.TradeRegistryNo = NullIfBlank(request.TradeRegistryNo);
        entity.Phone = NullIfBlank(request.Phone);
        entity.Email = NullIfBlank(request.Email);
        entity.IsEInvoiceUser = request.IsEInvoiceUser;
        entity.EInvoiceAlias = request.IsEInvoiceUser ? NullIfBlank(request.EInvoiceAlias) : null;
        entity.EInvoiceProfile = request.EInvoiceProfile;
        entity.IsEDespatchUser = request.IsEDespatchUser;
        entity.EDespatchAlias = request.IsEDespatchUser ? NullIfBlank(request.EDespatchAlias) : null;
        entity.EArchiveDelivery = request.EArchiveDelivery;
        entity.EArchiveEmail = NullIfBlank(request.EArchiveEmail);
        entity.Currency = CurrencyOf(request);
        entity.RequireUniqueCustomerOrderNo = request.RequireUniqueCustomerOrderNo;
        entity.PaymentTermDays = request.PaymentTermDays;
        entity.DocumentLanguage = request.DocumentLanguage;
        entity.IsActive = request.IsActive;
    }

    private static string CurrencyOf(CustomerRequest request) =>
        NullIfBlank(request.Currency)?.ToUpperInvariant() ?? "TRY";

    protected override Task<Error?> ValidateAsync(CustomerRequest request, Customer? existing, CancellationToken cancellationToken) =>
        CurrencyRules.RequireActiveAsync(Db, request.Currency, existing?.Currency, "currency", cancellationToken);

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.PriceLists.AnyAsync(p => p.CustomerId == id, cancellationToken))
        {
            return "fiyat listeleri";
        }

        if (await Db.Models.AnyAsync(m => m.CustomerId == id, cancellationToken))
        {
            return "modeller";
        }

        return await usage.FindAsync<Customer>(id, cancellationToken);
    }
}
