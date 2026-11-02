using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneWarehouseConfiguration : IEntityTypeConfiguration<BoyahaneWarehouse>
{
    public void Configure(EntityTypeBuilder<BoyahaneWarehouse> builder)
    {
        builder.ToTable("Warehouses", t => t.HasCheckConstraint("CK_Warehouses_Type", UpperSnakeEnumConverter.CheckConstraintSql<BoyahaneWarehouseType>("Type")));
        builder.Property(w => w.Code).HasMaxLength(30).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(w => w.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class BoyahaneWarehouseLocationConfiguration : IEntityTypeConfiguration<BoyahaneWarehouseLocation>
{
    public void Configure(EntityTypeBuilder<BoyahaneWarehouseLocation> builder)
    {
        builder.ToTable("WarehouseLocations");
        builder.Property(l => l.Code).HasMaxLength(40).IsRequired();
        builder.Property(l => l.Zone).HasMaxLength(10).IsRequired();
        builder.Property(l => l.Rack).HasMaxLength(10);
        builder.Property(l => l.Bin).HasMaxLength(10);
        builder.Property(l => l.Description).HasMaxLength(100);
        builder.HasOne(l => l.Warehouse).WithMany(w => w.Locations).HasForeignKey(l => l.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(l => new { l.WarehouseId, l.Code }).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class BoyahaneLocationMovementConfiguration : IEntityTypeConfiguration<BoyahaneLocationMovement>
{
    public void Configure(EntityTypeBuilder<BoyahaneLocationMovement> builder)
    {
        builder.ToTable("LocationMovements", t =>
        {
            t.HasCheckConstraint("CK_LocationMovements_GoodsType", UpperSnakeEnumConverter.CheckConstraintSql<WarehouseGoodsType>("GoodsType"));
            t.HasCheckConstraint("CK_LocationMovements_Type", UpperSnakeEnumConverter.CheckConstraintSql<LocationMovementType>("Type"));
        });
        builder.Property(m => m.GoodsNo).HasMaxLength(40).IsRequired();
        builder.Property(m => m.Note).HasMaxLength(200);
        builder.Property(m => m.CreatedBy).HasMaxLength(100);
        builder.HasOne(m => m.FromLocation).WithMany().HasForeignKey(m => m.FromLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.ToLocation).WithMany().HasForeignKey(m => m.ToLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => m.GoodsNo);
    }
}
