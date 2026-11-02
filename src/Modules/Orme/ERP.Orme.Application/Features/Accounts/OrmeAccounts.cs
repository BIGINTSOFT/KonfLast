using System.Globalization;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using ERP.Orme.Domain.Rules;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Accounts;

public sealed class OrmePaymentQuery : PageRequest
{
    /// <summary>Tahsilat no, müşteri ya da belge no.</summary>
    public string? Search { get; set; }
}

public sealed record OrmePaymentListItemDto(
    int Id,
    string PaymentNo,
    string CustomerName,
    DateOnly PaymentDate,
    PaymentMethod Method,
    string? ReferenceNo,
    DateOnly? DueDate,
    decimal Amount,
    decimal AllocatedAmount,
    string Currency,
    PaymentStatus Status);

public sealed record OrmePaymentAllocationDto(int Id, int InvoiceId, string InvoiceNo, DateOnly InvoiceDate, DateOnly? DueDate, decimal Amount);

public sealed record OrmePaymentDto(
    int Id,
    string PaymentNo,
    int CustomerId,
    string CustomerName,
    DateOnly PaymentDate,
    PaymentMethod Method,
    string? ReferenceNo,
    DateOnly? DueDate,
    decimal Amount,
    decimal AllocatedAmount,
    decimal UnallocatedAmount,
    string Currency,
    string? Notes,
    PaymentStatus Status,
    string? CancelReason,
    IReadOnlyList<OrmePaymentAllocationDto> Allocations);

/// <param name="Currency">Boşsa müşterinin para birimi.</param>
public sealed record OrmePaymentRequest(
    int CustomerId,
    DateOnly PaymentDate,
    PaymentMethod Method,
    decimal Amount,
    string? Currency = null,
    string? ReferenceNo = null,
    DateOnly? DueDate = null,
    string? Notes = null);

public sealed class OrmePaymentRequestValidator : AbstractValidator<OrmePaymentRequest>
{
    public OrmePaymentRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Müşteri seçilmelidir.");
        RuleFor(x => x.PaymentDate).NotEmpty().WithMessage("Tahsilat tarihi zorunludur.");
        RuleFor(x => x.Method).IsInEnum().WithMessage("Geçerli bir ödeme şekli seçin.");
        RuleFor(x => x.Amount).InclusiveBetween(0.01m, 999_999_999).WithMessage("Tutar sıfırdan büyük olmalıdır.");
        RuleFor(x => x.Currency).Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır.").When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.ReferenceNo).OptionalText(50);
        RuleFor(x => x.Notes).OptionalText(500);
        RuleFor(x => x.ReferenceNo).NotEmpty().WithMessage("Çek numarası zorunludur.").When(x => x.Method == PaymentMethod.Check);
        RuleFor(x => x.DueDate).NotNull().WithMessage("Çek vadesi zorunludur.").When(x => x.Method == PaymentMethod.Check);
    }
}

public sealed record OrmePaymentCancelRequest(string Reason);

public sealed class OrmePaymentCancelRequestValidator : AbstractValidator<OrmePaymentCancelRequest>
{
    public OrmePaymentCancelRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("İptal gerekçesi zorunludur.").MaximumLength(500);
    }
}

public sealed record OrmeAllocationLineRequest(int InvoiceId, decimal Amount);

public sealed record OrmeAllocationRequest(IReadOnlyList<OrmeAllocationLineRequest> Lines);

public sealed class OrmeAllocationRequestValidator : AbstractValidator<OrmeAllocationRequest>
{
    public OrmeAllocationRequestValidator()
    {
        RuleFor(x => x.Lines).NotEmpty().WithMessage("En az bir fatura seçilmelidir.");
        RuleFor(x => x.Lines).Must(lines => lines is null || lines.Select(l => l.InvoiceId).Distinct().Count() == lines.Count).WithMessage("Aynı fatura iki kez seçilemez.");
        RuleForEach(x => x.Lines).ChildRules(line => line.RuleFor(l => l.Amount).GreaterThan(0).WithMessage("Tutar sıfırdan büyük olmalıdır."));
    }
}

/// <param name="Balance">Faturalanan − tahsil edilen. Eksi ise müşteri avanslıdır.</param>
/// <param name="UnallocatedAmount">Henüz faturaya dağıtılmamış tahsilat.</param>
public sealed record OrmeAccountDto(
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    string Currency,
    decimal Invoiced,
    decimal Collected,
    decimal Balance,
    decimal OverdueAmount,
    decimal UnallocatedAmount,
    DateOnly? OldestOverdueDate);

public sealed record OrmeOpenInvoiceDto(
    int Id,
    string InvoiceNo,
    int CustomerId,
    string CustomerName,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string Currency,
    decimal Payable,
    decimal Paid,
    decimal Open,
    int DaysOverdue);

public sealed class OrmeOpenInvoiceQuery
{
    public int? CustomerId { get; set; }
    public string? Currency { get; set; }
    public bool OverdueOnly { get; set; }
}

public sealed class OrmeStatementQuery
{
    /// <summary>Boşsa müşterinin para birimi.</summary>
    public string? Currency { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public enum StatementEntryType
{
    Invoice,
    Payment,
}

public sealed record OrmeStatementRowDto(DateOnly Date, StatementEntryType Type, int DocumentId, string DocumentNo, DateOnly? DueDate, string? Description, decimal Debit, decimal Credit, decimal Balance);

public sealed record OrmeStatementDto(
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    string? CustomerTaxNumber,
    string? CustomerTaxOffice,
    string Currency,
    IReadOnlyList<string> Currencies,
    DateOnly? From,
    DateOnly? To,
    decimal OpeningBalance,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal ClosingBalance,
    IReadOnlyList<OrmeStatementRowDto> Rows);

/// <param name="Debit">Müşterinin borcu.</param>
/// <param name="Credit">Müşterinin alacağı (avans).</param>
/// <param name="AllocatedAmount">Tahsilatla eşleşen kısım; eşleşmiş kayıt silinemez.</param>
public sealed record OrmeOpeningBalanceDto(
    int Id,
    string DocumentNo,
    int CustomerId,
    string CustomerName,
    DateOnly Date,
    DateOnly? DueDate,
    string Currency,
    decimal Debit,
    decimal Credit,
    decimal AllocatedAmount,
    string? Notes);

/// <param name="IsCredit">Müşteri alacaklıysa doğru, borçluysa yanlış.</param>
/// <param name="Currency">Boşsa müşterinin para birimi.</param>
public sealed record OrmeOpeningBalanceRequest(
    int CustomerId,
    DateOnly Date,
    decimal Amount,
    bool IsCredit = false,
    string? Currency = null,
    DateOnly? DueDate = null,
    string? Notes = null);

public sealed class OrmeOpeningBalanceRequestValidator : AbstractValidator<OrmeOpeningBalanceRequest>
{
    public OrmeOpeningBalanceRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Müşteri seçilmelidir.");
        RuleFor(x => x.Date).NotEmpty().WithMessage("Tarih zorunludur.");
        RuleFor(x => x.Amount).InclusiveBetween(0.01m, 999_999_999).WithMessage("Tutar sıfırdan büyük olmalıdır.");
        RuleFor(x => x.Currency).Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır.").When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.Notes).OptionalText(500);
    }
}

public interface IOrmeAccountService
{
    Task<IReadOnlyList<OrmeOpeningBalanceDto>> GetOpeningBalancesAsync(string? search, CancellationToken cancellationToken);
    Task<Result<int>> CreateOpeningBalanceAsync(OrmeOpeningBalanceRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteOpeningBalanceAsync(bool isCredit, int id, CancellationToken cancellationToken);
    Task<PagedResult<OrmePaymentListItemDto>> GetPaymentsAsync(OrmePaymentQuery query, CancellationToken cancellationToken);
    Task<Result<OrmePaymentDto>> GetPaymentAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmePaymentDto>> CreatePaymentAsync(OrmePaymentRequest request, CancellationToken cancellationToken);
    Task<Result<OrmePaymentDto>> CancelPaymentAsync(int id, OrmePaymentCancelRequest request, CancellationToken cancellationToken);
    Task<Result<OrmePaymentDto>> AllocateAsync(int id, OrmeAllocationRequest request, CancellationToken cancellationToken);
    Task<Result<OrmePaymentDto>> RemoveAllocationAsync(int id, int allocationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrmeAccountDto>> GetAccountsAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrmeOpenInvoiceDto>> GetOpenInvoicesAsync(OrmeOpenInvoiceQuery query, CancellationToken cancellationToken);
    Task<Result<OrmeStatementDto>> GetStatementAsync(int customerId, OrmeStatementQuery query, CancellationToken cancellationToken);
}

/// <summary>Cari, ayrı bir hareket tablosu tutmaz: onaylı faturalar borç, etkin tahsilatlar alacaktır.</summary>
public sealed class OrmeAccountService(IAppDbContext db, IDocumentNumberGenerator numbers, IAppLock appLock, TimeProvider timeProvider) : IOrmeAccountService
{
    /// <summary>Dağıtım ve fatura geri alma aynı kilitle sıralanır; açık tutar iki kez kullanılamaz.</summary>
    public const string AccountLock = "OrmeAccounts";
    public static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private const string PaymentSeries = "OrmePayment";
    private const string OpeningSeries = "OrmeOpening";
    private const string OpeningText = "Açılış bakiyesi";
    private const string InvalidState = "PAYMENT_INVALID_STATE";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Tahsilat bulunamadı.", ErrorType.NotFound);
    private static readonly Error Busy = new(ErrorCodes.ResourceBusy, "Cari işlemi sürüyor, biraz sonra yeniden deneyin.", ErrorType.Conflict);

    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    private static string Money(decimal value) => value.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));

    public Task<PagedResult<OrmePaymentListItemDto>> GetPaymentsAsync(OrmePaymentQuery query, CancellationToken cancellationToken)
    {
        var payments = db.Set<OrmePayment>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            payments = payments.Where(p => p.PaymentNo.Contains(term) || p.Customer.Name.Contains(term) || (p.ReferenceNo != null && p.ReferenceNo.Contains(term)));
        }

        return payments
            .OrderByDescending(p => p.Id)
            .Select(p => new OrmePaymentListItemDto(
                p.Id,
                p.PaymentNo,
                p.Customer.Name,
                p.PaymentDate,
                p.Method,
                p.ReferenceNo,
                p.DueDate,
                p.Amount,
                p.Allocations.Sum(a => a.Amount),
                p.Currency,
                p.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<OrmePaymentDto>> GetPaymentAsync(int id, CancellationToken cancellationToken)
    {
        var payment = await db.Set<OrmePayment>().AsNoTracking()
            .Include(p => p.Customer)
            .Include(p => p.Allocations).ThenInclude(a => a.Invoice)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (payment is null)
        {
            return NotFound;
        }

        var allocated = payment.Allocations.Sum(a => a.Amount);
        return new OrmePaymentDto(
            payment.Id,
            payment.PaymentNo,
            payment.CustomerId,
            payment.Customer.Name,
            payment.PaymentDate,
            payment.Method,
            payment.ReferenceNo,
            payment.DueDate,
            payment.Amount,
            allocated,
            payment.Status == PaymentStatus.Active ? payment.Amount - allocated : 0,
            payment.Currency,
            payment.Notes,
            payment.Status,
            payment.CancelReason,
            payment.Allocations.OrderBy(a => a.Id)
                .Select(a => new OrmePaymentAllocationDto(a.Id, a.InvoiceId, a.Invoice.InvoiceNo, a.Invoice.InvoiceDate, a.Invoice.DueDate, a.Amount))
                .ToList());
    }

    public async Task<Result<OrmePaymentDto>> CreatePaymentAsync(OrmePaymentRequest request, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(db.Set<OrmeCustomer>(), request.CustomerId, null, "customerId", "Müşteri", cancellationToken) is { } customerError)
        {
            return customerError;
        }

        var customerCurrency = await db.Set<OrmeCustomer>().Where(c => c.Id == request.CustomerId).Select(c => c.Currency).SingleAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.PaymentDate.Year;
        var sequence = await numbers.NextAsync(PaymentSeries, year, cancellationToken);
        var payment = new OrmePayment
        {
            PaymentNo = $"OTH-{year}-{sequence:D5}",
            CustomerId = request.CustomerId,
            PaymentDate = request.PaymentDate,
            Method = request.Method,
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? customerCurrency : request.Currency.Trim().ToUpperInvariant(),
            ReferenceNo = NullIfBlank(request.ReferenceNo),
            DueDate = request.Method == PaymentMethod.Check ? request.DueDate : null,
            Notes = NullIfBlank(request.Notes),
        };
        db.Set<OrmePayment>().Add(payment);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetPaymentAsync(payment.Id, cancellationToken);
    }

    public async Task<Result<OrmePaymentDto>> CancelPaymentAsync(int id, OrmePaymentCancelRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(AccountLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var payment = await db.Set<OrmePayment>().Include(p => p.Allocations).SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (payment is null)
        {
            return NotFound;
        }

        if (payment.Status != PaymentStatus.Active)
        {
            return new Error(InvalidState, "Tahsilat zaten iptal edilmiş.", ErrorType.Conflict);
        }

        db.Set<OrmePaymentAllocation>().RemoveRange(payment.Allocations);
        payment.Status = PaymentStatus.Cancelled;
        payment.CancelReason = request.Reason.Trim();
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetPaymentAsync(id, cancellationToken);
    }

    public async Task<Result<OrmePaymentDto>> AllocateAsync(int id, OrmeAllocationRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(AccountLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var payment = await db.Set<OrmePayment>().Include(p => p.Allocations).SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (payment is null)
        {
            return NotFound;
        }

        if (payment.Status != PaymentStatus.Active)
        {
            return new Error(InvalidState, "İptal edilmiş tahsilat dağıtılamaz.", ErrorType.Conflict);
        }

        var unallocated = payment.Amount - payment.Allocations.Sum(a => a.Amount);
        if (request.Lines.Sum(l => l.Amount) > unallocated)
        {
            return new Error(InvalidState, $"Dağıtılan tutar tahsilatın kalanını ({Money(unallocated)} {payment.Currency}) aşıyor.", ErrorType.Conflict);
        }

        var ids = request.Lines.Select(l => l.InvoiceId).ToList();
        var invoices = await LoadInvoicesAsync(db.Set<OrmeInvoice>().Where(i => ids.Contains(i.Id)), cancellationToken);
        foreach (var line in request.Lines)
        {
            var invoice = invoices.SingleOrDefault(i => i.Id == line.InvoiceId);
            if (invoice is null || invoice.CustomerId != payment.CustomerId)
            {
                return new Error(InvalidState, "Fatura bu müşterinin onaylı faturası değil.", ErrorType.Conflict);
            }

            if (invoice.Currency != payment.Currency)
            {
                return new Error(InvalidState, $"{invoice.InvoiceNo} para birimi tahsilatla aynı değil.", ErrorType.Conflict);
            }

            if (line.Amount > invoice.Open)
            {
                return new Error(InvalidState, $"{invoice.InvoiceNo} için kalan {Money(invoice.Open)} {invoice.Currency}; fazlası dağıtılamaz.", ErrorType.Conflict);
            }

            var existing = payment.Allocations.SingleOrDefault(a => a.InvoiceId == line.InvoiceId);
            if (existing is not null)
            {
                existing.Amount += line.Amount;
            }
            else
            {
                payment.Allocations.Add(new OrmePaymentAllocation { InvoiceId = line.InvoiceId, Amount = line.Amount });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetPaymentAsync(id, cancellationToken);
    }

    public async Task<Result<OrmePaymentDto>> RemoveAllocationAsync(int id, int allocationId, CancellationToken cancellationToken)
    {
        var allocation = await db.Set<OrmePaymentAllocation>().SingleOrDefaultAsync(a => a.Id == allocationId && a.PaymentId == id, cancellationToken);
        if (allocation is null)
        {
            return NotFound;
        }

        db.Set<OrmePaymentAllocation>().Remove(allocation);
        await db.SaveChangesAsync(cancellationToken);
        return await GetPaymentAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<OrmeOpeningBalanceDto>> GetOpeningBalancesAsync(string? search, CancellationToken cancellationToken)
    {
        var term = NullIfBlank(search);
        var debts = await db.Set<OrmeInvoice>().AsNoTracking()
            .Where(i => i.OpeningAmount != null && (term == null || i.Customer.Name.Contains(term) || i.Customer.CustomerCode.Contains(term)))
            .Select(i => new OrmeOpeningBalanceDto(
                i.Id, i.InvoiceNo, i.CustomerId, i.Customer.Name, i.InvoiceDate, i.DueDate, i.Currency, i.OpeningAmount!.Value, 0,
                db.Set<OrmePaymentAllocation>().Where(a => a.InvoiceId == i.Id).Sum(a => (decimal?)a.Amount) ?? 0, i.Notes))
            .ToListAsync(cancellationToken);
        var credits = await db.Set<OrmePayment>().AsNoTracking()
            .Where(p => p.Method == PaymentMethod.Opening && p.Status == PaymentStatus.Active && (term == null || p.Customer.Name.Contains(term) || p.Customer.CustomerCode.Contains(term)))
            .Select(p => new OrmeOpeningBalanceDto(
                p.Id, p.PaymentNo, p.CustomerId, p.Customer.Name, p.PaymentDate, null, p.Currency, 0, p.Amount,
                p.Allocations.Sum(a => a.Amount), p.Notes))
            .ToListAsync(cancellationToken);
        return debts.Concat(credits).OrderBy(o => o.CustomerName).ThenBy(o => o.Currency).ThenBy(o => o.Date).ToList();
    }

    /// <summary>Açılış borcu vadesiyle faturalar arasına, açılış alacağı dağıtılabilir avans olarak tahsilatlar arasına girer. Sonuç: kaydın kimliği.</summary>
    public async Task<Result<int>> CreateOpeningBalanceAsync(OrmeOpeningBalanceRequest request, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(db.Set<OrmeCustomer>(), request.CustomerId, null, "customerId", "Müşteri", cancellationToken) is { } customerError)
        {
            return customerError;
        }

        var customerCurrency = await db.Set<OrmeCustomer>().Where(c => c.Id == request.CustomerId).Select(c => c.Currency).SingleAsync(cancellationToken);
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? customerCurrency : request.Currency.Trim().ToUpperInvariant();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.Date.Year;
        var sequence = await numbers.NextAsync(OpeningSeries, year, cancellationToken);
        var documentNo = $"OAB-{year}-{sequence:D5}";
        int id;
        if (request.IsCredit)
        {
            var payment = new OrmePayment
            {
                PaymentNo = documentNo,
                CustomerId = request.CustomerId,
                PaymentDate = request.Date,
                Method = PaymentMethod.Opening,
                Amount = request.Amount,
                Currency = currency,
                Notes = NullIfBlank(request.Notes),
            };
            db.Set<OrmePayment>().Add(payment);
            await db.SaveChangesAsync(cancellationToken);
            id = payment.Id;
        }
        else
        {
            var invoice = new OrmeInvoice
            {
                InvoiceNo = documentNo,
                CustomerId = request.CustomerId,
                InvoiceDate = request.Date,
                DueDate = request.DueDate,
                Currency = currency,
                Notes = NullIfBlank(request.Notes),
                Status = InvoiceStatus.Confirmed,
                OpeningAmount = request.Amount,
            };
            db.Set<OrmeInvoice>().Add(invoice);
            await db.SaveChangesAsync(cancellationToken);
            id = invoice.Id;
        }

        await transaction.CommitAsync(cancellationToken);
        return id;
    }

    public async Task<Result> DeleteOpeningBalanceAsync(bool isCredit, int id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(AccountLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var used = isCredit
            ? await db.Set<OrmePaymentAllocation>().AnyAsync(a => a.PaymentId == id, cancellationToken)
            : await db.Set<OrmePaymentAllocation>().AnyAsync(a => a.InvoiceId == id, cancellationToken);
        if (used)
        {
            return new Error(ErrorCodes.RecordInUse, "Tahsilatla eşleşmiş açılış kaydı silinemez; önce dağıtımı kaldırın.", ErrorType.Conflict);
        }

        if (isCredit)
        {
            var payment = await db.Set<OrmePayment>().SingleOrDefaultAsync(p => p.Id == id && p.Method == PaymentMethod.Opening, cancellationToken);
            if (payment is null)
            {
                return new Error(ErrorCodes.NotFound, "Açılış kaydı bulunamadı.", ErrorType.NotFound);
            }

            db.Set<OrmePayment>().Remove(payment);
        }
        else
        {
            var invoice = await db.Set<OrmeInvoice>().SingleOrDefaultAsync(i => i.Id == id && i.OpeningAmount != null, cancellationToken);
            if (invoice is null)
            {
                return new Error(ErrorCodes.NotFound, "Açılış kaydı bulunamadı.", ErrorType.NotFound);
            }

            db.Set<OrmeInvoice>().Remove(invoice);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<OrmeAccountDto>> GetAccountsAsync(string? search, CancellationToken cancellationToken)
    {
        var invoices = await LoadInvoicesAsync(db.Set<OrmeInvoice>(), cancellationToken);
        var payments = await db.Set<OrmePayment>().AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Active)
            .Select(p => new { p.CustomerId, p.Currency, p.Amount, Allocated = p.Allocations.Sum(a => a.Amount) })
            .ToListAsync(cancellationToken);
        var customers = await db.Set<OrmeCustomer>().AsNoTracking()
            .Select(c => new { c.Id, c.CustomerCode, c.Name })
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var keys = invoices.Select(i => (i.CustomerId, i.Currency)).Union(payments.Select(p => (p.CustomerId, p.Currency))).ToList();
        var term = NullIfBlank(search);
        return keys
            .Where(k => customers.ContainsKey(k.CustomerId))
            .Where(k => term is null
                || customers[k.CustomerId].Name.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                || customers[k.CustomerId].CustomerCode.Contains(term, StringComparison.CurrentCultureIgnoreCase))
            .Select(k =>
            {
                var own = invoices.Where(i => i.CustomerId == k.CustomerId && i.Currency == k.Currency).ToList();
                var paid = payments.Where(p => p.CustomerId == k.CustomerId && p.Currency == k.Currency).ToList();
                var overdue = own.Where(i => i.Open > 0 && i.DueDate < Today).ToList();
                var invoiced = own.Sum(i => i.Payable);
                var collected = paid.Sum(p => p.Amount);
                return new OrmeAccountDto(
                    k.CustomerId,
                    customers[k.CustomerId].CustomerCode,
                    customers[k.CustomerId].Name,
                    k.Currency,
                    invoiced,
                    collected,
                    invoiced - collected,
                    overdue.Sum(i => i.Open),
                    paid.Sum(p => p.Amount - p.Allocated),
                    overdue.Min(i => i.DueDate));
            })
            .OrderBy(a => a.CustomerName).ThenBy(a => a.Currency)
            .ToList();
    }

    public async Task<IReadOnlyList<OrmeOpenInvoiceDto>> GetOpenInvoicesAsync(OrmeOpenInvoiceQuery query, CancellationToken cancellationToken)
    {
        var source = db.Set<OrmeInvoice>().AsQueryable();
        if (query.CustomerId is { } customerId)
        {
            source = source.Where(i => i.CustomerId == customerId);
        }

        if (NullIfBlank(query.Currency) is { } currency)
        {
            source = source.Where(i => i.Currency == currency);
        }

        var invoices = await LoadInvoicesAsync(source, cancellationToken);
        return invoices
            .Where(i => i.Open > 0 && (!query.OverdueOnly || i.DueDate < Today))
            .OrderBy(i => i.DueDate ?? DateOnly.MaxValue).ThenBy(i => i.InvoiceDate).ThenBy(i => i.Id)
            .Select(i => new OrmeOpenInvoiceDto(
                i.Id,
                i.InvoiceNo,
                i.CustomerId,
                i.CustomerName,
                i.InvoiceDate,
                i.DueDate,
                i.Currency,
                i.Payable,
                i.Paid,
                i.Open,
                i.DueDate is { } due && due < Today ? Today.DayNumber - due.DayNumber : 0))
            .ToList();
    }

    public async Task<Result<OrmeStatementDto>> GetStatementAsync(int customerId, OrmeStatementQuery query, CancellationToken cancellationToken)
    {
        var customer = await db.Set<OrmeCustomer>().AsNoTracking().SingleOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        if (customer is null)
        {
            return new Error(ErrorCodes.NotFound, "Müşteri bulunamadı.", ErrorType.NotFound);
        }

        var invoices = await LoadInvoicesAsync(db.Set<OrmeInvoice>().Where(i => i.CustomerId == customerId), cancellationToken);
        var payments = await db.Set<OrmePayment>().AsNoTracking()
            .Where(p => p.CustomerId == customerId && p.Status == PaymentStatus.Active)
            .ToListAsync(cancellationToken);
        var currencies = invoices.Select(i => i.Currency).Union(payments.Select(p => p.Currency)).Order().ToList();
        var currency = NullIfBlank(query.Currency)?.ToUpperInvariant() ?? customer.Currency;

        var entries = invoices.Where(i => i.Currency == currency)
            .Select(i => (Date: i.InvoiceDate, Order: 0, i.Id, Type: StatementEntryType.Invoice, No: i.InvoiceNo, i.DueDate, Description: i.IsOpening ? OpeningText : "Fatura", Debit: i.Payable, Credit: 0m))
            .Concat(payments.Where(p => p.Currency == currency)
                .Select(p => (Date: p.PaymentDate, Order: 1, p.Id, Type: StatementEntryType.Payment, No: p.PaymentNo, p.DueDate, Description: p.Method == PaymentMethod.Opening ? OpeningText : $"Tahsilat {p.ReferenceNo}".Trim(), Debit: 0m, Credit: p.Amount)))
            .OrderBy(e => e.Date).ThenBy(e => e.Order).ThenBy(e => e.Id)
            .ToList();

        var opening = entries.Where(e => e.Date < query.From).Sum(e => e.Debit - e.Credit);
        var balance = opening;
        var rows = new List<OrmeStatementRowDto>();
        foreach (var entry in entries.Where(e => !(e.Date < query.From) && !(e.Date > query.To)))
        {
            balance += entry.Debit - entry.Credit;
            rows.Add(new OrmeStatementRowDto(entry.Date, entry.Type, entry.Id, entry.No, entry.DueDate, entry.Description, entry.Debit, entry.Credit, balance));
        }

        return new OrmeStatementDto(
            customer.Id,
            customer.CustomerCode,
            customer.Name,
            customer.TaxNumber,
            customer.TaxOffice,
            currency,
            currencies,
            query.From,
            query.To,
            opening,
            rows.Sum(r => r.Debit),
            rows.Sum(r => r.Credit),
            balance,
            rows);
    }

    private sealed record InvoiceBalance(int Id, string InvoiceNo, int CustomerId, string CustomerName, DateOnly InvoiceDate, DateOnly? DueDate, string Currency, decimal Payable, decimal Paid, bool IsOpening)
    {
        public decimal Open => Payable - Paid;
    }

    /// <summary>Onaylı faturaların ödenecek tutarı fatura ekranındaki hesapla aynı kuralla bulunur.</summary>
    private async Task<List<InvoiceBalance>> LoadInvoicesAsync(IQueryable<OrmeInvoice> source, CancellationToken cancellationToken)
    {
        var rows = await source.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Confirmed)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNo,
                i.CustomerId,
                CustomerName = i.Customer.Name,
                i.InvoiceDate,
                i.DueDate,
                i.Currency,
                i.VatRate,
                i.WithholdingPercent,
                i.OpeningAmount,
                Lines = i.Lines.Select(l => new { l.Quantity, l.UnitPrice, l.DiscountPercent }).ToList(),
                Paid = db.Set<OrmePaymentAllocation>().Where(a => a.InvoiceId == i.Id).Sum(a => (decimal?)a.Amount) ?? 0,
            })
            .ToListAsync(cancellationToken);
        return rows
            .Select(r => new InvoiceBalance(
                r.Id,
                r.InvoiceNo,
                r.CustomerId,
                r.CustomerName,
                r.InvoiceDate,
                r.DueDate,
                r.Currency,
                r.OpeningAmount ?? InvoiceAmounts.Of(r.Lines.Sum(l => InvoiceAmounts.Line(l.Quantity, l.UnitPrice ?? 0, l.DiscountPercent)), r.VatRate, r.WithholdingPercent).Payable,
                r.Paid,
                r.OpeningAmount is not null))
            .ToList();
    }
}
