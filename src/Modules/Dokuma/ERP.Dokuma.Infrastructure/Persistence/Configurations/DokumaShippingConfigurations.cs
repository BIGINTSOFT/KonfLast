using ERP.Core.Infrastructure.Persistence.Converters;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Dokuma.Infrastructure.Persistence.Configurations;

public sealed class DokumaPackingListConfiguration : IEntityTypeConfiguration<DokumaPackingList>
{
    public void Configure(EntityTypeBuilder<DokumaPackingList> builder)
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

public sealed class DokumaPackingItemConfiguration : IEntityTypeConfiguration<DokumaPackingItem>
{
    public void Configure(EntityTypeBuilder<DokumaPackingItem> builder)
    {
        builder.ToTable("PackingItems", t =>
        {
            t.HasCheckConstraint("CK_PackingItems_Meters", "[Meters] > 0");
            t.HasCheckConstraint("CK_PackingItems_Kilograms", "[Kilograms] IS NULL OR [Kilograms] > 0");
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

public sealed class DokumaDespatchConfiguration : IEntityTypeConfiguration<DokumaDespatch>
{
    public void Configure(EntityTypeBuilder<DokumaDespatch> builder)
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

public sealed class DokumaInvoiceConfiguration : IEntityTypeConfiguration<DokumaInvoice>
{
    public void Configure(EntityTypeBuilder<DokumaInvoice> builder)
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

public sealed class DokumaInvoiceLineConfiguration : IEntityTypeConfiguration<DokumaInvoiceLine>
{
    public void Configure(EntityTypeBuilder<DokumaInvoiceLine> builder)
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

public sealed class DokumaPaymentConfiguration : IEntityTypeConfiguration<DokumaPayment>
{
    public void Configure(EntityTypeBuilder<DokumaPayment> builder)
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

public sealed class DokumaPaymentAllocationConfiguration : IEntityTypeConfiguration<DokumaPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<DokumaPaymentAllocation> builder)
    {
        builder.ToTable("PaymentAllocations", t => t.HasCheckConstraint("CK_PaymentAllocations_Amount", "[Amount] > 0"));
        builder.Property(a => a.Amount).HasPrecision(16, 2);
        builder.HasOne(a => a.Payment).WithMany(p => p.Allocations).HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Invoice).WithMany().HasForeignKey(a => a.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.PaymentId, a.InvoiceId }).IsUnique();
        builder.HasIndex(a => a.InvoiceId);
    }
}

public sealed class DokumaYarnReturnConfiguration : IEntityTypeConfiguration<DokumaYarnReturn>
{
    public void Configure(EntityTypeBuilder<DokumaYarnReturn> builder)
    {
        builder.ToTable("YarnReturns", t => t.HasCheckConstraint("CK_YarnReturns_Kilograms", "[Kilograms] > 0"));
        builder.Property(r => r.ReturnNo).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Kilograms).HasPrecision(12, 2);
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.HasOne(r => r.YarnLot).WithMany(l => l.Returns).HasForeignKey(r => r.YarnLotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.ReturnNo).IsUnique();
    }
}

public sealed class DokumaYarnTakeBackConfiguration : IEntityTypeConfiguration<DokumaYarnTakeBack>
{
    public void Configure(EntityTypeBuilder<DokumaYarnTakeBack> builder)
    {
        builder.ToTable("YarnTakeBacks", t =>
        {
            t.HasCheckConstraint("CK_YarnTakeBacks_Kilograms", "[Kilograms] > 0");
            t.HasCheckConstraint("CK_YarnTakeBacks_Source", "([WarpOrderId] IS NULL AND [BeamId] IS NOT NULL) OR ([WarpOrderId] IS NOT NULL AND [BeamId] IS NULL)");
        });
        builder.Property(t => t.Kilograms).HasPrecision(12, 2);
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.HasOne(t => t.WarpOrder).WithMany(w => w.TakeBacks).HasForeignKey(t => t.WarpOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Beam).WithMany(b => b.TakeBacks).HasForeignKey(t => t.BeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.YarnLot).WithMany(l => l.TakeBacks).HasForeignKey(t => t.YarnLotId).OnDelete(DeleteBehavior.Restrict);
    }
}
