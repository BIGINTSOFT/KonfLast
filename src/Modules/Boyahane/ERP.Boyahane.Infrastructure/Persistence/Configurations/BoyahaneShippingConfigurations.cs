using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneDespatchConfiguration : IEntityTypeConfiguration<BoyahaneDespatch>
{
    public void Configure(EntityTypeBuilder<BoyahaneDespatch> builder)
    {
        builder.ToTable("Despatches", t => t.HasCheckConstraint("CK_Despatches_Status", UpperSnakeEnumConverter.CheckConstraintSql<DespatchStatus>("Status")));
        builder.Property(d => d.DespatchNo).HasMaxLength(30).IsRequired();
        builder.Property(d => d.VehiclePlate).HasMaxLength(20);
        builder.Property(d => d.DriverName).HasMaxLength(100);
        builder.Property(d => d.DeliveryAddress).HasMaxLength(500);
        builder.Property(d => d.Notes).HasMaxLength(1000);
        builder.HasOne(d => d.Customer).WithMany().HasForeignKey(d => d.CustomerId);
        builder.HasOne(d => d.Invoice).WithMany(i => i.Despatches).HasForeignKey(d => d.InvoiceId);
        builder.HasMany(d => d.PackingLists).WithOne(l => l.Despatch).HasForeignKey(l => l.DespatchId);
        builder.HasIndex(d => d.DespatchNo).IsUnique();
    }
}

public sealed class BoyahaneInvoiceConfiguration : IEntityTypeConfiguration<BoyahaneInvoice>
{
    public void Configure(EntityTypeBuilder<BoyahaneInvoice> builder)
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
        builder.HasOne(i => i.Customer).WithMany().HasForeignKey(i => i.CustomerId);
        builder.HasIndex(i => i.InvoiceNo).IsUnique();
    }
}

public sealed class BoyahaneInvoiceLineConfiguration : IEntityTypeConfiguration<BoyahaneInvoiceLine>
{
    public void Configure(EntityTypeBuilder<BoyahaneInvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines", t => t.HasCheckConstraint("CK_InvoiceLines_Unit", UpperSnakeEnumConverter.CheckConstraintSql<StockUnit>("Unit")));
        builder.Property(l => l.Description).HasMaxLength(300).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(14, 2);
        builder.Property(l => l.UnitPrice).HasPrecision(14, 4);
        builder.HasOne(l => l.Invoice).WithMany(i => i.Lines).HasForeignKey(l => l.InvoiceId);
        builder.HasOne(l => l.OrderLine).WithMany().HasForeignKey(l => l.OrderLineId);
    }
}
