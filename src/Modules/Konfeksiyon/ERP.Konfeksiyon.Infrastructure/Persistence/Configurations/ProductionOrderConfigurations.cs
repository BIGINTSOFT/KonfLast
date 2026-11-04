using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class ProductionOrderConfiguration : IEntityTypeConfiguration<ProductionOrder>
{
    public void Configure(EntityTypeBuilder<ProductionOrder> builder)
    {
        builder.ToTable("ProductionOrders", t =>
        {
            t.HasCheckConstraint(
                "CK_ProductionOrders_Status", UpperSnakeEnumConverter.CheckConstraintSql<ProductionOrderStatus>("Status"));
            t.HasCheckConstraint("CK_ProductionOrders_DueDate", "[DueDate] >= [OrderDate]");
            t.HasCheckConstraint(
                "CK_ProductionOrders_OperationSequence",
                UpperSnakeEnumConverter.CheckConstraintSql<OperationSequenceMode>("OperationSequence"));
        });

        builder.Property(o => o.OrderNo).HasMaxLength(30).IsRequired();
        builder.Property(o => o.CustomerOrderNo).HasMaxLength(50);
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId);
        builder.HasOne(o => o.Product).WithMany().HasForeignKey(o => o.ProductId);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(o => o.MaterialWarehouseId);
        builder.HasOne(o => o.SalesOrder).WithMany().HasForeignKey(o => o.SalesOrderId);

        // Numara silinen siparişlerde de yeniden kullanılmaz: filtre yok.
        builder.HasIndex(o => o.OrderNo).IsUnique();
        builder.HasIndex(o => new { o.Status, o.DueDate });
        builder.HasIndex(o => o.DueDate);
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.ProductId);
        builder.HasIndex(o => o.MaterialWarehouseId);
        builder.HasIndex(o => o.SalesOrderId);
    }
}

public sealed class ProductionOrderItemConfiguration : IEntityTypeConfiguration<ProductionOrderItem>
{
    public void Configure(EntityTypeBuilder<ProductionOrderItem> builder)
    {
        builder.ToTable(
            "ProductionOrderItems", t => t.HasCheckConstraint("CK_ProductionOrderItems_Quantity", "[Quantity] > 0"));

        builder.HasOne(i => i.ProductionOrder).WithMany(o => o.Items).HasForeignKey(i => i.ProductionOrderId);
        builder.HasOne(i => i.Color).WithMany().HasForeignKey(i => i.ColorId);
        builder.HasOne(i => i.Size).WithMany().HasForeignKey(i => i.SizeId);

        builder.HasIndex(i => new { i.ProductionOrderId, i.ColorId, i.SizeId }).IsUnique();
        builder.HasIndex(i => i.ColorId);
        builder.HasIndex(i => i.SizeId);

        builder.HasQueryFilter(i => !i.ProductionOrder.IsDeleted);
    }
}

public sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders", t =>
        {
            t.HasCheckConstraint("CK_SalesOrders_Status", UpperSnakeEnumConverter.CheckConstraintSql<SalesOrderStatus>("Status"));
            t.HasCheckConstraint("CK_SalesOrders_DueDate", "[DueDate] >= [OrderDate]");
        });

        builder.Property(o => o.OrderNo).HasMaxLength(30).IsRequired();
        builder.Property(o => o.CustomerOrderNo).HasMaxLength(50);
        builder.Property(o => o.Currency).HasMaxLength(3).IsUnicode(false).IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.SubmittedBy).HasMaxLength(100);
        builder.Property(o => o.DecidedBy).HasMaxLength(100);
        builder.Property(o => o.DecisionNote).HasMaxLength(500);
        builder.Property(o => o.ClosedBy).HasMaxLength(100);
        builder.Property(o => o.CloseNote).HasMaxLength(500);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId);
        builder.HasOne(o => o.CustomerBuyer).WithMany().HasForeignKey(o => o.CustomerBuyerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.DeliveryAddress).WithMany().HasForeignKey(o => o.DeliveryAddressId).OnDelete(DeleteBehavior.Restrict);

        // Numara silinen siparişlerde de yeniden kullanılmaz: filtre yok.
        builder.HasIndex(o => o.OrderNo).IsUnique();
        builder.HasIndex(o => new { o.Status, o.DueDate });
        builder.HasIndex(o => o.CustomerId);
    }
}

public sealed class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.ToTable("SalesOrderLines", t =>
        {
            t.HasCheckConstraint("CK_SalesOrderLines_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_SalesOrderLines_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
        });

        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.ShippedFromReservation).HasPrecision(18, 3);

        builder.HasOne(l => l.SalesOrder).WithMany(o => o.Lines).HasForeignKey(l => l.SalesOrderId);
        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId);
        builder.HasOne(l => l.Color).WithMany().HasForeignKey(l => l.ColorId);
        builder.HasOne(l => l.Size).WithMany().HasForeignKey(l => l.SizeId);

        builder.HasIndex(l => new { l.SalesOrderId, l.ProductId, l.ColorId, l.SizeId }).IsUnique();
        builder.HasIndex(l => l.ProductId);
        builder.HasIndex(l => l.ColorId);
        builder.HasIndex(l => l.SizeId);

        builder.HasQueryFilter(l => !l.SalesOrder.IsDeleted);
    }
}

public sealed class SalesOrderReservationConfiguration : IEntityTypeConfiguration<SalesOrderReservation>
{
    public void Configure(EntityTypeBuilder<SalesOrderReservation> builder)
    {
        builder.ToTable("SalesOrderReservations", t => t.HasCheckConstraint("CK_SalesOrderReservations_Quantity", "[Quantity] > 0"));

        builder.Property(r => r.Quantity).HasPrecision(18, 3);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100).IsRequired();

        builder.HasOne(r => r.SalesOrder).WithMany().HasForeignKey(r => r.SalesOrderId);
        builder.HasOne(r => r.SalesOrderLine).WithMany().HasForeignKey(r => r.SalesOrderLineId);
        builder.HasOne(r => r.Warehouse).WithMany().HasForeignKey(r => r.WarehouseId);
        builder.HasOne(r => r.StockItem).WithMany().HasForeignKey(r => r.StockItemId);

        // Kalem başına depo başına kaynak (stok / üretim) başına tek ayırma; stok kontrolü (depo, kart) ile sorgular.
        builder.HasIndex(r => new { r.SalesOrderLineId, r.WarehouseId, r.FromProduction }).IsUnique();
        builder.HasIndex(r => new { r.WarehouseId, r.StockItemId });
    }
}
