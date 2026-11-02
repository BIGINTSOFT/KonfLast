using ERP.Core.Infrastructure.Persistence.Converters;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Orme.Infrastructure.Persistence.Configurations;

public sealed class OrmePurchaseOrderConfiguration : IEntityTypeConfiguration<OrmePurchaseOrder>
{
    public void Configure(EntityTypeBuilder<OrmePurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders", t => t.HasCheckConstraint("CK_PurchaseOrders_Status", UpperSnakeEnumConverter.CheckConstraintSql<PurchaseOrderStatus>("Status")));

        builder.Property(o => o.OrderNo).HasMaxLength(30).IsRequired();
        builder.Property(o => o.Currency).HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(1000);

        builder.HasOne(o => o.Supplier).WithMany().HasForeignKey(o => o.SupplierId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.OrderNo).IsUnique();
        builder.HasIndex(o => o.Status);
    }
}

public sealed class OrmePurchaseOrderLineConfiguration : IEntityTypeConfiguration<OrmePurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<OrmePurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines", t =>
        {
            t.HasCheckConstraint("CK_PurchaseOrderLines_Kilograms", "[Kilograms] > 0");
            t.HasCheckConstraint("CK_PurchaseOrderLines_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
        });

        builder.Property(l => l.Kilograms).HasPrecision(12, 2);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.HasOne(l => l.Order).WithMany(o => o.Lines).HasForeignKey(l => l.OrderId);
        builder.HasOne(l => l.Yarn).WithMany().HasForeignKey(l => l.YarnId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.YarnId);
    }
}
