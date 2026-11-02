using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders", t =>
        {
            t.HasCheckConstraint(
                "CK_PurchaseOrders_Status", UpperSnakeEnumConverter.CheckConstraintSql<PurchaseOrderStatus>("Status"));
            t.HasCheckConstraint("CK_PurchaseOrders_ExpectedDate", "[ExpectedDate] IS NULL OR [ExpectedDate] >= [OrderDate]");
            t.HasCheckConstraint("CK_PurchaseOrders_ExchangeRate", "[ExchangeRate] IS NULL OR [ExchangeRate] > 0");
        });

        builder.Property(o => o.OrderNo).HasMaxLength(30).IsRequired();
        builder.Property(o => o.Currency).HasMaxLength(3).IsUnicode(false).IsRequired();
        builder.Property(o => o.ExchangeRate).HasPrecision(18, 6);
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasOne(o => o.Supplier).WithMany().HasForeignKey(o => o.SupplierId);

        // Numara silinen siparişlerde de yeniden kullanılmaz: filtre yok.
        builder.HasIndex(o => o.OrderNo).IsUnique();
        builder.HasIndex(o => new { o.Status, o.ExpectedDate });
        builder.HasIndex(o => o.SupplierId);
    }
}

public sealed class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("PurchaseOrderItems", t =>
        {
            t.HasCheckConstraint(
                "CK_PurchaseOrderItems_Quantities",
                "[Quantity] > 0 AND [ReceivedQuantity] >= 0 AND [ReceivedQuantity] <= [Quantity]");
            t.HasCheckConstraint("CK_PurchaseOrderItems_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
        });

        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.ReceivedQuantity).HasPrecision(18, 3);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 4);
        builder.Ignore(i => i.RemainingQuantity);

        builder.HasOne(i => i.PurchaseOrder).WithMany(o => o.Items).HasForeignKey(i => i.PurchaseOrderId);
        builder.HasOne(i => i.StockItem).WithMany().HasForeignKey(i => i.StockItemId);
        builder.HasOne(i => i.ProductionOrder).WithMany().HasForeignKey(i => i.ProductionOrderId);

        // Bir stok kartı siparişte tek kalemdir.
        builder.HasIndex(i => new { i.PurchaseOrderId, i.StockItemId }).IsUnique();
        builder.HasIndex(i => i.StockItemId);
        builder.HasIndex(i => i.ProductionOrderId);

        builder.HasQueryFilter(i => !i.PurchaseOrder.IsDeleted);
    }
}

public sealed class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.ToTable("GoodsReceipts");

        builder.Property(r => r.DocumentNo).HasMaxLength(50);
        builder.Property(r => r.Note).HasMaxLength(300);
        builder.Property(r => r.ReceivedBy).HasMaxLength(100).IsRequired();

        builder.HasOne(r => r.PurchaseOrder).WithMany().HasForeignKey(r => r.PurchaseOrderId);
        builder.HasOne(r => r.Warehouse).WithMany().HasForeignKey(r => r.WarehouseId);
        builder.HasMany(r => r.Lines).WithOne(l => l.GoodsReceipt).HasForeignKey(l => l.GoodsReceiptId);

        // Aynı istek yeniden gelirse mal kabul ikinci kez yazılamaz.
        builder.HasIndex(r => r.RequestId).IsUnique();
        builder.HasIndex(r => new { r.PurchaseOrderId, r.Id });
        builder.HasIndex(r => r.WarehouseId);
    }
}

public sealed class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptLine> builder)
    {
        builder.ToTable("GoodsReceiptLines", t => t.HasCheckConstraint("CK_GoodsReceiptLines_Quantity", "[Quantity] > 0"));

        builder.Property(l => l.Quantity).HasPrecision(18, 3);

        builder.HasOne(l => l.PurchaseOrderItem).WithMany().HasForeignKey(l => l.PurchaseOrderItemId);

        builder.HasIndex(l => l.GoodsReceiptId);
        builder.HasIndex(l => l.PurchaseOrderItemId);
    }
}
