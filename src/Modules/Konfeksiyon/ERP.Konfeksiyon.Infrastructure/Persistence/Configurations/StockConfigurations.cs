using ERP.Konfeksiyon.Domain.Entities;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ERP.Core.Infrastructure.Persistence.Configurations;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    /// <summary>Hazır gelen depolar. Id'ler sabittir; yenileri kullanıcı tarafından eklenir.</summary>
    private static readonly (int Id, string Code, string Name, WarehouseType Type)[] Defaults =
    [
        (1, "HAMMADDE", "Hammadde Deposu", WarehouseType.RawMaterial),
        (2, "MAMUL", "Mamul Deposu", WarehouseType.FinishedGoods),
    ];

    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses", t => t.HasCheckConstraint(
            "CK_Warehouses_Type", UpperSnakeEnumConverter.CheckConstraintSql<WarehouseType>("Type")));

        builder.Property(w => w.Code).HasMaxLength(30).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(w => w.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);

        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(Defaults.Select(w => new Warehouse
        {
            Id = w.Id, Code = w.Code, Name = w.Name, Type = w.Type, IsActive = true, CreatedAt = seededAt,
            CreatedBy = "system",
        }));
    }
}

public sealed class WarehouseLocationConfiguration : IEntityTypeConfiguration<WarehouseLocation>
{
    public void Configure(EntityTypeBuilder<WarehouseLocation> builder)
    {
        builder.ToTable("WarehouseLocations");

        builder.Property(l => l.Code).HasMaxLength(32).IsRequired();
        builder.Property(l => l.Zone).HasMaxLength(10).IsRequired();
        builder.Property(l => l.Rack).HasMaxLength(10);
        builder.Property(l => l.Bin).HasMaxLength(10);
        builder.Property(l => l.Description).HasMaxLength(100);

        builder.HasOne(l => l.Warehouse).WithMany(w => w.Locations).HasForeignKey(l => l.WarehouseId);

        builder.HasIndex(l => new { l.WarehouseId, l.Code }).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);

        // Her deponun tek bir genel adresi vardır.
        builder.HasIndex(l => l.WarehouseId)
            .IsUnique()
            .HasDatabaseName("UX_WarehouseLocations_Default")
            .HasFilter("[IsDefault] = 1 AND [IsDeleted] = 0");
    }
}

public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems", t =>
        {
            t.HasCheckConstraint(
                "CK_StockItems_ItemType", UpperSnakeEnumConverter.CheckConstraintSql<StockItemType>("ItemType"));
            t.HasCheckConstraint("CK_StockItems_Unit", UpperSnakeEnumConverter.CheckConstraintSql<StockUnit>("Unit"));

            // Mamul kartı ürün + renk + bedeni taşır; diğer kartlarda bu alanlar boştur.
            t.HasCheckConstraint(
                "CK_StockItems_FinishedGood",
                "([ItemType] = 'FINISHED_GOOD' AND [ProductId] IS NOT NULL AND [ColorId] IS NOT NULL AND [SizeId] IS NOT NULL) " +
                "OR ([ItemType] <> 'FINISHED_GOOD' AND [ProductId] IS NULL AND [ColorId] IS NULL AND [SizeId] IS NULL)");
            t.HasCheckConstraint(
                "CK_StockItems_Reorder",
                "([MinimumStock] IS NULL OR [MinimumStock] >= 0) AND ([ReorderQuantity] IS NULL OR [ReorderQuantity] > 0)");
        });

        // Mamul kodu ürün, renk ve beden kodlarından oluşur.
        builder.Property(i => i.Code).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(300).IsRequired();
        builder.Property(i => i.UnitCost).HasPrecision(18, 4);
        builder.Property(i => i.MinimumStock).HasPrecision(18, 3);
        builder.Property(i => i.ReorderQuantity).HasPrecision(18, 3);
        builder.Ignore(i => i.IsFinishedGood);

        builder.HasOne(i => i.Supplier).WithMany().HasForeignKey(i => i.SupplierId);

        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId);
        builder.HasOne(i => i.Color).WithMany().HasForeignKey(i => i.ColorId);
        builder.HasOne(i => i.Size).WithMany().HasForeignKey(i => i.SizeId);

        builder.HasIndex(i => i.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);

        // Bir ürünün bir renk ve bedeni için tek mamul kartı.
        builder.HasIndex(i => new { i.ProductId, i.ColorId, i.SizeId })
            .IsUnique()
            .HasDatabaseName("UX_StockItems_FinishedGood")
            .HasFilter("[ProductId] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(i => i.ItemType);
        builder.HasIndex(i => i.ColorId);
        builder.HasIndex(i => i.SizeId);
    }
}

public sealed class ProductMaterialConfiguration : IEntityTypeConfiguration<ProductMaterial>
{
    public void Configure(EntityTypeBuilder<ProductMaterial> builder)
    {
        builder.ToTable("ProductMaterials", t =>
        {
            t.HasCheckConstraint("CK_ProductMaterials_QuantityPerPiece", "[QuantityPerPiece] > 0");
            t.HasCheckConstraint("CK_ProductMaterials_WastePercent", "[WastePercent] >= 0 AND [WastePercent] <= 100");
        });

        builder.Property(m => m.QuantityPerPiece).HasPrecision(18, 4);
        builder.Property(m => m.WastePercent).HasPrecision(5, 2);
        builder.Ignore(m => m.Specificity);
        builder.Ignore(m => m.GrossQuantityPerPiece);

        builder.HasOne(m => m.Product).WithMany().HasForeignKey(m => m.ProductId);
        builder.HasOne(m => m.StockItem).WithMany().HasForeignKey(m => m.StockItemId);
        builder.HasOne(m => m.Color).WithMany().HasForeignKey(m => m.ColorId);
        builder.HasOne(m => m.Size).WithMany().HasForeignKey(m => m.SizeId);

        builder.HasIndex(m => new { m.ProductId, m.StockItemId });
        builder.HasIndex(m => m.StockItemId);
        builder.HasIndex(m => m.ColorId);
        builder.HasIndex(m => m.SizeId);
    }
}

public sealed class StockLotConfiguration : IEntityTypeConfiguration<StockLot>
{
    public void Configure(EntityTypeBuilder<StockLot> builder)
    {
        builder.ToTable("StockLots", t =>
        {
            t.HasCheckConstraint("CK_StockLots_InitialQuantity", "[InitialQuantity] > 0");
            t.HasCheckConstraint("CK_StockLots_Width", "[Width] IS NULL OR [Width] > 0");
        });

        // Numara kimlikten türetilir: eşzamanlı girişlerde sıra çakışması olmaz.
        builder.Property(l => l.LotNo)
            .HasMaxLength(12)
            .HasComputedColumnSql("CONCAT(N'T', RIGHT(CONCAT('00000000', [Id]), 8))", stored: true);
        builder.Property(l => l.BatchNo).HasMaxLength(50);
        builder.Property(l => l.Shade).HasMaxLength(30);
        builder.Property(l => l.Width).HasPrecision(9, 2);
        builder.Property(l => l.SupplierRollNo).HasMaxLength(50);
        builder.Property(l => l.InitialQuantity).HasPrecision(18, 3);

        builder.HasOne(l => l.StockItem).WithMany().HasForeignKey(l => l.StockItemId);
        builder.HasOne(l => l.PurchaseOrder).WithMany().HasForeignKey(l => l.PurchaseOrderId);

        builder.HasIndex(l => l.LotNo).IsUnique();
        builder.HasIndex(l => new { l.StockItemId, l.Id });
        builder.HasIndex(l => l.BatchNo);
        builder.HasIndex(l => l.PurchaseOrderId);
    }
}

public sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("Stocks", t => t.HasCheckConstraint("CK_Stocks_Quantity", "[Quantity] >= 0"));

        builder.Property(s => s.Quantity).HasPrecision(18, 3);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasOne(s => s.Warehouse).WithMany().HasForeignKey(s => s.WarehouseId);
        builder.HasOne(s => s.Location).WithMany().HasForeignKey(s => s.LocationId);
        builder.HasOne(s => s.StockItem).WithMany().HasForeignKey(s => s.StockItemId);
        builder.HasOne(s => s.Lot).WithMany().HasForeignKey(s => s.LotId);

        // Bir kartın bir adreste (takipli kartta: topun o adreste) tek bakiyesi olur. Top boş olan satırlar da
        // tekildir: SQL Server tekil index'te NULL değerleri eşit sayar.
        builder.HasIndex(s => new { s.LocationId, s.StockItemId, s.LotId }).IsUnique().HasFilter(null);
        builder.HasIndex(s => s.LotId);
        builder.HasIndex(s => new { s.WarehouseId, s.StockItemId });
        builder.HasIndex(s => s.StockItemId);
    }
}

public sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("StockReservations", t => t.HasCheckConstraint("CK_StockReservations_Quantity", "[Quantity] > 0"));

        builder.Property(r => r.Quantity).HasPrecision(18, 3);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100).IsRequired();

        builder.HasOne(r => r.ProductionOrder).WithMany().HasForeignKey(r => r.ProductionOrderId);
        builder.HasOne(r => r.Warehouse).WithMany().HasForeignKey(r => r.WarehouseId);
        builder.HasOne(r => r.StockItem).WithMany().HasForeignKey(r => r.StockItemId);

        // Bir siparişin bir depoda bir kart için tek rezervasyonu olur.
        builder.HasIndex(r => new { r.ProductionOrderId, r.WarehouseId, r.StockItemId }).IsUnique();
        // Kullanılabilir stok hesabı: kartın depodaki tüm rezervasyonları.
        builder.HasIndex(r => new { r.WarehouseId, r.StockItemId });
        builder.HasIndex(r => r.StockItemId);
    }
}

public sealed class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> builder)
    {
        builder.ToTable("StockCounts", t => t.HasCheckConstraint(
            "CK_StockCounts_Status", UpperSnakeEnumConverter.CheckConstraintSql<StockCountStatus>("Status")));

        builder.Property(c => c.CountNo).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Note).HasMaxLength(300);
        builder.Property(c => c.PostedBy).HasMaxLength(100);
        builder.Property(c => c.RowVersion).IsRowVersion();
        builder.Ignore(c => c.IsOpen);

        builder.HasOne(c => c.Warehouse).WithMany().HasForeignKey(c => c.WarehouseId);
        builder.HasOne(c => c.Location).WithMany().HasForeignKey(c => c.LocationId);
        builder.HasMany(c => c.Lines).WithOne(l => l.StockCount).HasForeignKey(l => l.StockCountId);

        builder.HasIndex(c => c.CountNo).IsUnique();
        // Stok hareketlerinde "bu adreste açık sayım var mı" sorgusu.
        builder.HasIndex(c => new { c.WarehouseId, c.Status });
        builder.HasIndex(c => c.LocationId);
    }
}

public sealed class StockCountLineConfiguration : IEntityTypeConfiguration<StockCountLine>
{
    public void Configure(EntityTypeBuilder<StockCountLine> builder)
    {
        builder.ToTable("StockCountLines", t => t.HasCheckConstraint(
            "CK_StockCountLines_Quantities", "[ExpectedQuantity] >= 0 AND ([CountedQuantity] IS NULL OR [CountedQuantity] >= 0)"));

        builder.Property(l => l.ExpectedQuantity).HasPrecision(18, 3);
        builder.Property(l => l.CountedQuantity).HasPrecision(18, 3);
        builder.Ignore(l => l.Difference);

        builder.HasOne(l => l.Location).WithMany().HasForeignKey(l => l.LocationId);
        builder.HasOne(l => l.StockItem).WithMany().HasForeignKey(l => l.StockItemId);
        builder.HasOne(l => l.Lot).WithMany().HasForeignKey(l => l.LotId);

        // Fişte bir kart (ve top) bir adres için tek satırdır; top boş olan satırlar da tekildir.
        builder.HasIndex(l => new { l.StockCountId, l.LocationId, l.StockItemId, l.LotId }).IsUnique().HasFilter(null);
        builder.HasIndex(l => l.LocationId);
        builder.HasIndex(l => l.StockItemId);
        builder.HasIndex(l => l.LotId);
    }
}

public sealed class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public const string AppendOnlyTrigger = "TR_StockTransactions_AppendOnly";

    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.ToTable("StockTransactions", t =>
        {
            t.HasCheckConstraint(
                "CK_StockTransactions_Type", UpperSnakeEnumConverter.CheckConstraintSql<StockTransactionType>("Type"));
            t.HasCheckConstraint(
                "CK_StockTransactions_Quantity", "[Quantity] <> 0 AND [BalanceAfter] >= 0 AND [LocationBalanceAfter] >= 0");

            // Hareketler değiştirilemez / silinemez; trigger migration'da oluşturulur.
            t.HasTrigger(AppendOnlyTrigger);
        });

        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 3);
        builder.Property(x => x.LocationBalanceAfter).HasPrecision(18, 3);
        builder.Property(x => x.LotBalanceAfter).HasPrecision(18, 3);
        builder.Property(x => x.UnitCost).HasPrecision(18, 4);
        builder.Property(x => x.DocumentNo).HasMaxLength(50);
        builder.Property(x => x.Note).HasMaxLength(300);
        builder.Property(x => x.CreatedBy).HasMaxLength(100).IsRequired();

        builder.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId);
        builder.HasOne(x => x.CounterWarehouse).WithMany().HasForeignKey(x => x.CounterWarehouseId);
        builder.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId);
        builder.HasOne(x => x.CounterLocation).WithMany().HasForeignKey(x => x.CounterLocationId);
        builder.HasOne(x => x.Lot).WithMany().HasForeignKey(x => x.LotId);
        builder.HasOne(x => x.StockItem).WithMany().HasForeignKey(x => x.StockItemId);
        builder.HasOne(x => x.Bundle).WithMany().HasForeignKey(x => x.BundleId);
        builder.HasOne(x => x.ProductionOrder).WithMany().HasForeignKey(x => x.ProductionOrderId);
        builder.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId);
        builder.HasOne(x => x.Shipment).WithMany().HasForeignKey(x => x.ShipmentId);

        // Aynı istek yeniden gelirse hareket ikinci kez yazılamaz (transferin iki satırı ayrı türdedir).
        builder.HasIndex(x => new { x.RequestId, x.Type }).IsUnique();

        // Bir demet depoya yalnızca bir kez teslim alınır.
        builder.HasIndex(x => x.BundleId)
            .IsUnique()
            .HasDatabaseName("UX_StockTransactions_ReceiptPerBundle")
            .HasFilter("[BundleId] IS NOT NULL AND [Type] = 'PRODUCTION_RECEIPT'");

        // Tablo sürekli büyür: kart / depo ekstresi ve tarih bazlı listeler için.
        builder.HasIndex(x => new { x.StockItemId, x.WarehouseId, x.Id });
        builder.HasIndex(x => new { x.WarehouseId, x.Id });
        builder.HasIndex(x => new { x.OccurredAt, x.Type });
        builder.HasIndex(x => x.CounterWarehouseId);
        builder.HasIndex(x => new { x.LocationId, x.Id });
        builder.HasIndex(x => x.CounterLocationId);
        builder.HasIndex(x => new { x.LotId, x.Id });
        builder.HasIndex(x => x.ProductionOrderId);
        // İptal edilen demetin malzeme çıkışları bu index ile bulunur.
        builder.HasIndex(x => new { x.BundleId, x.Type });
        builder.HasIndex(x => x.PurchaseOrderId);
        builder.HasIndex(x => x.ShipmentId);
    }
}
