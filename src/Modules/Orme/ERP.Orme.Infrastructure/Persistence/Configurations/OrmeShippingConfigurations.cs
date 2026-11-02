using ERP.Core.Infrastructure.Persistence.Converters;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Orme.Infrastructure.Persistence.Configurations;

public sealed class OrmePackingListConfiguration : IEntityTypeConfiguration<OrmePackingList>
{
    public void Configure(EntityTypeBuilder<OrmePackingList> builder)
    {
        builder.ToTable("PackingLists", t => t.HasCheckConstraint("CK_PackingLists_Status", UpperSnakeEnumConverter.CheckConstraintSql<PackingListStatus>("Status")));
        builder.Property(l => l.PackingNo).HasMaxLength(30).IsRequired();
        builder.Property(l => l.Notes).HasMaxLength(1000);
        builder.Property(l => l.BoyahaneReceiptNo).HasMaxLength(30);
        builder.HasOne(l => l.Order).WithMany().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.SaleCustomer).WithMany().HasForeignKey(l => l.SaleCustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(l => l.PackingNo).IsUnique();
    }
}

public sealed class OrmePackingItemConfiguration : IEntityTypeConfiguration<OrmePackingItem>
{
    public void Configure(EntityTypeBuilder<OrmePackingItem> builder)
    {
        builder.ToTable("PackingItems", t =>
        {
            t.HasCheckConstraint("CK_PackingItems_Meters", "[Meters] > 0");
            t.HasCheckConstraint("CK_PackingItems_Kilograms", "[Kilograms] > 0");
            t.HasCheckConstraint("CK_PackingItems_Grade", "[Grade] IN ('FIRST_QUALITY', 'SECOND_QUALITY')");
        });

        builder.Property(i => i.Meters).HasPrecision(12, 2);
        builder.Property(i => i.Kilograms).HasPrecision(12, 2);

        builder.HasOne(i => i.PackingList).WithMany(l => l.Items).HasForeignKey(i => i.PackingListId);
        builder.HasOne(i => i.Roll).WithMany().HasForeignKey(i => i.RollId).OnDelete(DeleteBehavior.Restrict);

        // Bir top aynı anda tek listede olur.
        builder.HasIndex(i => i.RollId, "UX_PackingItems_Active").IsUnique().HasFilter("[IsActive] = 1");
    }
}

public sealed class OrmeDespatchConfiguration : IEntityTypeConfiguration<OrmeDespatch>
{
    public void Configure(EntityTypeBuilder<OrmeDespatch> builder)
    {
        builder.ToTable("Despatches", t => t.HasCheckConstraint("CK_Despatches_Status", UpperSnakeEnumConverter.CheckConstraintSql<DespatchStatus>("Status")));
        builder.Property(d => d.DespatchNo).HasMaxLength(30).IsRequired();
        builder.Property(d => d.VehiclePlate).HasMaxLength(20);
        builder.Property(d => d.DriverName).HasMaxLength(100);
        builder.Property(d => d.DeliveryAddress).HasMaxLength(500);
        builder.Property(d => d.Notes).HasMaxLength(1000);
        builder.HasOne(d => d.Customer).WithMany().HasForeignKey(d => d.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.Invoice).WithMany(i => i.Despatches).HasForeignKey(d => d.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(d => d.PackingLists).WithOne(l => l.Despatch).HasForeignKey(l => l.DespatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(d => d.DespatchNo).IsUnique();
    }
}

public sealed class OrmeInvoiceConfiguration : IEntityTypeConfiguration<OrmeInvoice>
{
    public void Configure(EntityTypeBuilder<OrmeInvoice> builder)
    {
        builder.ToTable("Invoices", t =>
        {
            t.HasCheckConstraint("CK_Invoices_Status", UpperSnakeEnumConverter.CheckConstraintSql<InvoiceStatus>("Status"));
            t.HasCheckConstraint("CK_Invoices_VatRate", "[VatRate] IS NULL OR [VatRate] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_Invoices_Withholding", "[WithholdingPercent] IS NULL OR [WithholdingPercent] BETWEEN 0 AND 100");
        });
        builder.Property(i => i.InvoiceNo).HasMaxLength(30).IsRequired();
        builder.Property(i => i.Currency).HasMaxLength(3).IsRequired();
        builder.Property(i => i.VatRate).HasPrecision(5, 2);
        builder.Property(i => i.WithholdingPercent).HasPrecision(5, 2);
        builder.Property(i => i.OpeningAmount).HasPrecision(16, 2);
        builder.Property(i => i.Notes).HasMaxLength(1000);
        builder.HasOne(i => i.Customer).WithMany().HasForeignKey(i => i.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.InvoiceNo).IsUnique();
    }
}

public sealed class OrmeInvoiceLineConfiguration : IEntityTypeConfiguration<OrmeInvoiceLine>
{
    public void Configure(EntityTypeBuilder<OrmeInvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines", t =>
        {
            t.HasCheckConstraint("CK_InvoiceLines_Grade", "[Grade] IN ('FIRST_QUALITY', 'SECOND_QUALITY')");
            t.HasCheckConstraint("CK_InvoiceLines_Discount", "[DiscountPercent] IS NULL OR [DiscountPercent] BETWEEN 0 AND 100");
        });
        builder.Property(l => l.Description).HasMaxLength(300).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(14, 2);
        builder.Property(l => l.UnitPrice).HasPrecision(14, 4);
        builder.Property(l => l.DiscountPercent).HasPrecision(5, 2);
        builder.HasOne(l => l.Invoice).WithMany(i => i.Lines).HasForeignKey(l => l.InvoiceId);
        builder.HasOne(l => l.OrderLine).WithMany().HasForeignKey(l => l.OrderLineId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrmePaymentConfiguration : IEntityTypeConfiguration<OrmePayment>
{
    public void Configure(EntityTypeBuilder<OrmePayment> builder)
    {
        builder.ToTable("Payments", t =>
        {
            t.HasCheckConstraint("CK_Payments_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_Payments_Method", UpperSnakeEnumConverter.CheckConstraintSql<PaymentMethod>("Method"));
            t.HasCheckConstraint("CK_Payments_Status", UpperSnakeEnumConverter.CheckConstraintSql<PaymentStatus>("Status"));
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

public sealed class OrmePaymentAllocationConfiguration : IEntityTypeConfiguration<OrmePaymentAllocation>
{
    public void Configure(EntityTypeBuilder<OrmePaymentAllocation> builder)
    {
        builder.ToTable("PaymentAllocations", t => t.HasCheckConstraint("CK_PaymentAllocations_Amount", "[Amount] > 0"));
        builder.Property(a => a.Amount).HasPrecision(16, 2);
        builder.HasOne(a => a.Payment).WithMany(p => p.Allocations).HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Invoice).WithMany().HasForeignKey(a => a.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.PaymentId, a.InvoiceId }).IsUnique();
        builder.HasIndex(a => a.InvoiceId);
    }
}

public sealed class OrmeYarnReturnConfiguration : IEntityTypeConfiguration<OrmeYarnReturn>
{
    public void Configure(EntityTypeBuilder<OrmeYarnReturn> builder)
    {
        builder.ToTable("YarnReturns", t => t.HasCheckConstraint("CK_YarnReturns_Kilograms", "[Kilograms] > 0"));
        builder.Property(r => r.ReturnNo).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Kilograms).HasPrecision(12, 2);
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.HasOne(r => r.YarnLot).WithMany(l => l.Returns).HasForeignKey(r => r.YarnLotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.ReturnNo).IsUnique();
    }
}
