using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneSupplierConfiguration : IEntityTypeConfiguration<BoyahaneSupplier>
{
    public void Configure(EntityTypeBuilder<BoyahaneSupplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.Property(s => s.Code).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.TaxNumber).HasMaxLength(20);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(150);
        builder.Property(s => s.Currency).HasMaxLength(3).IsRequired();
        builder.HasIndex(s => s.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class BoyahanePurchaseOrderConfiguration : IEntityTypeConfiguration<BoyahanePurchaseOrder>
{
    public void Configure(EntityTypeBuilder<BoyahanePurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders", t => t.HasCheckConstraint("CK_PurchaseOrders_Status", UpperSnakeEnumConverter.CheckConstraintSql<PurchaseOrderStatus>("Status")));
        builder.Property(o => o.OrderNo).HasMaxLength(30).IsRequired();
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.HasOne(o => o.Supplier).WithMany().HasForeignKey(o => o.SupplierId);
        builder.HasIndex(o => o.OrderNo).IsUnique();
    }
}

public sealed class BoyahanePurchaseOrderLineConfiguration : IEntityTypeConfiguration<BoyahanePurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<BoyahanePurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines", t => t.HasCheckConstraint("CK_PurchaseOrderLines_Kilograms", "[Kilograms] > 0"));
        builder.Property(l => l.Kilograms).HasPrecision(14, 3);
        builder.Property(l => l.ReceivedKilograms).HasPrecision(14, 3);
        builder.Property(l => l.UnitPrice).HasPrecision(14, 4);
        builder.HasOne(l => l.Order).WithMany(o => o.Lines).HasForeignKey(l => l.OrderId);
        builder.HasOne(l => l.DyeChemical).WithMany().HasForeignKey(l => l.DyeChemicalId);
    }
}
