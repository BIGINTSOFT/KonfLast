using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneChemicalLotConfiguration : IEntityTypeConfiguration<BoyahaneChemicalLot>
{
    public void Configure(EntityTypeBuilder<BoyahaneChemicalLot> builder)
    {
        builder.ToTable("ChemicalLots", t =>
        {
            t.HasCheckConstraint("CK_ChemicalLots_Received", "[ReceivedKilograms] > 0");
            t.HasCheckConstraint("CK_ChemicalLots_Remaining", "[RemainingKilograms] >= 0");
        });
        builder.Property(l => l.LotNo).HasMaxLength(30).IsRequired();
        builder.Property(l => l.SupplierLotNo).HasMaxLength(50);
        builder.Property(l => l.SupplierName).HasMaxLength(150);
        builder.Property(l => l.ReceivedKilograms).HasPrecision(14, 3);
        builder.Property(l => l.RemainingKilograms).HasPrecision(14, 3);
        builder.Property(l => l.UnitCost).HasPrecision(14, 4);
        builder.Property(l => l.Currency).HasMaxLength(3).IsRequired();
        builder.Property(l => l.Notes).HasMaxLength(500);
        builder.HasOne(l => l.DyeChemical).WithMany().HasForeignKey(l => l.DyeChemicalId);
        builder.HasOne(l => l.Supplier).WithMany().HasForeignKey(l => l.SupplierId);
        builder.HasOne(l => l.PurchaseOrderLine).WithMany().HasForeignKey(l => l.PurchaseOrderLineId);
        builder.HasIndex(l => l.LotNo).IsUnique();
        builder.HasIndex(l => new { l.DyeChemicalId, l.ExpiryDate });
    }
}

public sealed class BoyahaneChemicalMovementConfiguration : IEntityTypeConfiguration<BoyahaneChemicalMovement>
{
    public void Configure(EntityTypeBuilder<BoyahaneChemicalMovement> builder)
    {
        builder.ToTable("ChemicalMovements", t => t.HasCheckConstraint("CK_ChemicalMovements_Type", UpperSnakeEnumConverter.CheckConstraintSql<ChemicalMovementType>("Type")));
        builder.Property(m => m.Kilograms).HasPrecision(14, 3);
        builder.Property(m => m.Reference).HasMaxLength(200);
        builder.Property(m => m.CreatedBy).HasMaxLength(100);
        builder.HasOne(m => m.Lot).WithMany().HasForeignKey(m => m.LotId);
    }
}

public sealed class BoyahaneConsumptionLotConfiguration : IEntityTypeConfiguration<BoyahaneConsumptionLot>
{
    public void Configure(EntityTypeBuilder<BoyahaneConsumptionLot> builder)
    {
        builder.ToTable("ConsumptionLots");
        builder.Property(c => c.Kilograms).HasPrecision(14, 3);
        builder.HasOne(c => c.Consumption).WithMany(x => x.Lots).HasForeignKey(c => c.ConsumptionId);
        builder.HasOne(c => c.Lot).WithMany().HasForeignKey(c => c.LotId);
    }
}
