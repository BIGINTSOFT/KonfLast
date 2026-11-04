using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments", t =>
        {
            t.HasCheckConstraint("CK_Shipments_Status", UpperSnakeEnumConverter.CheckConstraintSql<ShipmentStatus>("Status"));

            // Sevk edilmiş sevkiyatın ne zaman ve kim tarafından sevk edildiği bellidir.
            t.HasCheckConstraint(
                "CK_Shipments_Shipped", "[Status] <> 'SHIPPED' OR ([ShippedAt] IS NOT NULL AND [ShippedBy] IS NOT NULL)");
        });

        builder.Property(s => s.ShipmentNo).HasMaxLength(30).IsRequired();
        builder.Property(s => s.DocumentNo).HasMaxLength(50);
        builder.Property(s => s.Carrier).HasMaxLength(150);
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.Property(s => s.ShippedBy).HasMaxLength(100);
        builder.Property(s => s.PickingBy).HasMaxLength(100);
        builder.Property(s => s.PickedBy).HasMaxLength(100);
        builder.Property(s => s.VehiclePlate).HasMaxLength(20);
        builder.Property(s => s.DriverName).HasMaxLength(100);
        builder.Property(s => s.DriverIdentityNo).HasMaxLength(11);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasOne(s => s.Customer).WithMany().HasForeignKey(s => s.CustomerId);
        builder.HasOne(s => s.Warehouse).WithMany().HasForeignKey(s => s.WarehouseId);
        builder.HasOne(s => s.ProductionOrder).WithMany().HasForeignKey(s => s.ProductionOrderId);
        builder.HasOne(s => s.SalesOrder).WithMany().HasForeignKey(s => s.SalesOrderId);
        builder.HasOne(s => s.Invoice).WithMany(i => i.Shipments).HasForeignKey(s => s.InvoiceId).OnDelete(DeleteBehavior.Restrict);

        // Numara silinen sevkiyatlarda da yeniden kullanılmaz: filtre yok.
        builder.HasIndex(s => s.ShipmentNo).IsUnique();
        builder.HasIndex(s => new { s.Status, s.ShipDate });
        builder.HasIndex(s => s.CustomerId);
        builder.HasIndex(s => s.WarehouseId);
        builder.HasIndex(s => new { s.ProductionOrderId, s.Status });
        builder.HasIndex(s => new { s.SalesOrderId, s.Status });
        builder.HasIndex(s => s.ShippedAt);
        builder.HasIndex(s => s.InvoiceId);
    }
}

public sealed class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItem>
{
    public void Configure(EntityTypeBuilder<ShipmentItem> builder)
    {
        builder.ToTable("ShipmentItems", t => t.HasCheckConstraint("CK_ShipmentItems_Quantity", "[Quantity] > 0"));

        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.RequestedQuantity).HasPrecision(18, 3);
        builder.Property(i => i.PickedQuantity).HasPrecision(18, 3);
        builder.Property(i => i.CheckedQuantity).HasPrecision(18, 3);

        builder.HasOne(i => i.Shipment).WithMany(s => s.Items).HasForeignKey(i => i.ShipmentId);
        builder.HasOne(i => i.StockItem).WithMany().HasForeignKey(i => i.StockItemId);

        // Bir mamul kartı sevkiyatta tek kalemdir.
        builder.HasIndex(i => new { i.ShipmentId, i.StockItemId }).IsUnique();
        builder.HasIndex(i => i.StockItemId);

        builder.HasQueryFilter(i => !i.Shipment.IsDeleted);
    }
}

public sealed class ShipmentPickConfiguration : IEntityTypeConfiguration<ShipmentPick>
{
    public void Configure(EntityTypeBuilder<ShipmentPick> builder)
    {
        builder.ToTable("ShipmentPicks", t => t.HasCheckConstraint("CK_ShipmentPicks_Quantity", "[Quantity] > 0"));

        builder.Property(p => p.Quantity).HasPrecision(18, 3);
        builder.Property(p => p.PickedBy).HasMaxLength(100).IsRequired();

        builder.HasOne(p => p.Shipment).WithMany(s => s.Picks).HasForeignKey(p => p.ShipmentId);
        // İki yol da sevkiyata çıkar (zincirleme silme çakışır); kayıtlar kalemden önce kodda silinir.
        builder.HasOne(p => p.ShipmentItem).WithMany().HasForeignKey(p => p.ShipmentItemId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(p => p.Location).WithMany().HasForeignKey(p => p.LocationId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.RequestId).IsUnique();
        builder.HasIndex(p => p.ShipmentId);
        builder.HasIndex(p => p.ShipmentItemId);

        builder.HasQueryFilter(p => !p.Shipment.IsDeleted);
    }
}
