using ERP.Core.Infrastructure.Persistence.Converters;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Dokuma.Infrastructure.Persistence.Configurations;

public sealed class DokumaPurchaseOrderConfiguration : IEntityTypeConfiguration<DokumaPurchaseOrder>
{
    public void Configure(EntityTypeBuilder<DokumaPurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders", t =>
            t.HasCheckConstraint("CK_PurchaseOrders_Status", UpperSnakeEnumConverter.CheckConstraintSql<PurchaseOrderStatus>("Status")));

        builder.Property(o => o.OrderNo).HasMaxLength(30).IsRequired();
        builder.Property(o => o.Currency).HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.ApprovedBy).HasMaxLength(100);

        builder.HasOne(o => o.Supplier).WithMany().HasForeignKey(o => o.SupplierId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.OrderNo).IsUnique();
        builder.HasIndex(o => o.OrderDate);
        builder.HasIndex(o => o.Status);
    }
}

public sealed class DokumaPurchaseOrderLineConfiguration : IEntityTypeConfiguration<DokumaPurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<DokumaPurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines", t =>
        {
            t.HasCheckConstraint("CK_PurchaseOrderLines_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_PurchaseOrderLines_ReceivedQuantity", "[ReceivedQuantity] >= 0");
            t.HasCheckConstraint("CK_PurchaseOrderLines_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
            t.HasCheckConstraint("CK_PurchaseOrderLines_Unit", UpperSnakeEnumConverter.CheckConstraintSql<MaterialUnit>("Unit"));
            // Satır ya iplik ya da malzemedir; iplik kilogramla alınır.
            t.HasCheckConstraint(
                "CK_PurchaseOrderLines_Item",
                "([YarnId] IS NOT NULL AND [MaterialId] IS NULL AND [Unit] = 'KILOGRAM') OR ([YarnId] IS NULL AND [MaterialId] IS NOT NULL)");
        });

        builder.Property(l => l.Quantity).HasPrecision(14, 3);
        builder.Property(l => l.ReceivedQuantity).HasPrecision(14, 3);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.HasOne(l => l.Order).WithMany(o => o.Lines).HasForeignKey(l => l.OrderId);
        builder.HasOne(l => l.Yarn).WithMany().HasForeignKey(l => l.YarnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Material).WithMany().HasForeignKey(l => l.MaterialId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.OrderId, l.Sequence }).IsUnique();
    }
}
