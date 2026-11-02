using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices", t =>
        {
            t.HasCheckConstraint("CK_Invoices_Status", UpperSnakeEnumConverter.CheckConstraintSql<InvoiceStatus>("Status"));
            t.HasCheckConstraint("CK_Invoices_Withholding", "[WithholdingPercent] IS NULL OR [WithholdingPercent] BETWEEN 0 AND 100");
        });
        builder.Property(i => i.InvoiceNo).HasMaxLength(30).IsRequired();
        builder.Property(i => i.Currency).HasMaxLength(3).IsRequired();
        builder.Property(i => i.WithholdingPercent).HasPrecision(5, 2);
        builder.Property(i => i.OpeningAmount).HasPrecision(16, 2);
        builder.Property(i => i.Notes).HasMaxLength(1000);
        builder.HasOne(i => i.Customer).WithMany().HasForeignKey(i => i.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.InvoiceNo).IsUnique();
        builder.HasIndex(i => new { i.CustomerId, i.Status });
    }
}

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines", t =>
        {
            t.HasCheckConstraint("CK_InvoiceLines_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_InvoiceLines_Discount", "[DiscountPercent] IS NULL OR [DiscountPercent] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_InvoiceLines_VatRate", "[VatRate] BETWEEN 0 AND 100");
        });
        builder.Property(l => l.Description).HasMaxLength(300).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(18, 3);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.DiscountPercent).HasPrecision(5, 2);
        builder.Property(l => l.VatRate).HasPrecision(5, 2);
        builder.HasOne(l => l.Invoice).WithMany(i => i.Lines).HasForeignKey(l => l.InvoiceId);
        builder.HasOne(l => l.StockItem).WithMany().HasForeignKey(l => l.StockItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.SalesOrderLine).WithMany().HasForeignKey(l => l.SalesOrderLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(l => l.StockItemId);
        builder.HasIndex(l => l.SalesOrderLineId);
    }
}

public sealed class CustomerPaymentConfiguration : IEntityTypeConfiguration<CustomerPayment>
{
    public void Configure(EntityTypeBuilder<CustomerPayment> builder)
    {
        builder.ToTable("CustomerPayments", t =>
        {
            t.HasCheckConstraint("CK_CustomerPayments_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_CustomerPayments_Method", UpperSnakeEnumConverter.CheckConstraintSql<PaymentMethod>("Method"));
            t.HasCheckConstraint("CK_CustomerPayments_Status", UpperSnakeEnumConverter.CheckConstraintSql<PaymentStatus>("Status"));
        });
        builder.Property(p => p.PaymentNo).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(16, 2);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.ReferenceNo).HasMaxLength(50);
        builder.Property(p => p.Notes).HasMaxLength(500);
        builder.Property(p => p.CancelReason).HasMaxLength(500);
        builder.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.PaymentNo).IsUnique();
        builder.HasIndex(p => new { p.CustomerId, p.Currency, p.PaymentDate });
    }
}

public sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("PaymentAllocations", t => t.HasCheckConstraint("CK_PaymentAllocations_Amount", "[Amount] > 0"));
        builder.Property(a => a.Amount).HasPrecision(16, 2);
        builder.HasOne(a => a.Payment).WithMany(p => p.Allocations).HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Invoice).WithMany().HasForeignKey(a => a.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.PaymentId, a.InvoiceId }).IsUnique();
        builder.HasIndex(a => a.InvoiceId);
    }
}
