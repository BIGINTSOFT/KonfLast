using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneFabricReceiptConfiguration : IEntityTypeConfiguration<BoyahaneFabricReceipt>
{
    public void Configure(EntityTypeBuilder<BoyahaneFabricReceipt> builder)
    {
        builder.ToTable("FabricReceipts");

        builder.Property(r => r.ReceiptNo).HasMaxLength(30).IsRequired();
        builder.Property(r => r.DespatchNo).HasMaxLength(50);
        builder.Property(r => r.Notes).HasMaxLength(1000);

        builder.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId);

        builder.HasIndex(r => r.ReceiptNo).IsUnique();
        builder.HasIndex(r => r.ReceiptDate);
    }
}

public sealed class BoyahaneFabricLotConfiguration : IEntityTypeConfiguration<BoyahaneFabricLot>
{
    public void Configure(EntityTypeBuilder<BoyahaneFabricLot> builder)
    {
        builder.ToTable("FabricLots");

        builder.Property(l => l.LotNo).HasMaxLength(40).IsRequired();
        builder.Property(l => l.CustomerFabricCode).HasMaxLength(100);
        builder.Property(l => l.DeclaredMeters).HasPrecision(12, 2);
        builder.Property(l => l.DeclaredKilograms).HasPrecision(12, 2);

        builder.HasOne(l => l.Receipt).WithMany(r => r.Lots).HasForeignKey(l => l.ReceiptId);
        builder.HasOne(l => l.Fabric).WithMany().HasForeignKey(l => l.FabricId);

        builder.HasIndex(l => l.LotNo).IsUnique();
    }
}

public sealed class BoyahaneFabricRollConfiguration : IEntityTypeConfiguration<BoyahaneFabricRoll>
{
    public void Configure(EntityTypeBuilder<BoyahaneFabricRoll> builder)
    {
        builder.ToTable("FabricRolls", t =>
        {
            t.HasCheckConstraint("CK_FabricRolls_Status", UpperSnakeEnumConverter.CheckConstraintSql<FabricRollStatus>("Status"));
            t.HasCheckConstraint("CK_FabricRolls_Meters", "[Meters] > 0");
            t.HasCheckConstraint("CK_FabricRolls_Kilograms", "[Kilograms] > 0");
        });

        builder.Property(t => t.RollNo).HasMaxLength(30).IsRequired();
        builder.Property(t => t.CustomerRollNo).HasMaxLength(50);
        builder.Property(t => t.Meters).HasPrecision(12, 2);
        builder.Property(t => t.Kilograms).HasPrecision(12, 2);
        builder.Property(t => t.InspectedMeters).HasPrecision(12, 2);
        builder.Property(t => t.InspectedKilograms).HasPrecision(12, 2);
        builder.Property(t => t.InspectedWidthCm).HasPrecision(6, 2);
        builder.Property(t => t.InspectedBy).HasMaxLength(100);
        builder.Property(t => t.InspectionNote).HasMaxLength(1000);
        builder.Property(t => t.CustomerDecisionBy).HasMaxLength(100);
        builder.Property(t => t.CustomerDecisionNote).HasMaxLength(500);
        builder.Property(t => t.CustomerDecisionRecordedBy).HasMaxLength(100);

        builder.HasOne(t => t.Lot).WithMany(l => l.Rolls).HasForeignKey(t => t.LotId);
        builder.HasOne(t => t.OrderLine).WithMany(l => l.Rolls).HasForeignKey(t => t.OrderLineId);

        builder.HasIndex(t => t.RollNo).IsUnique();
        builder.HasIndex(t => t.Status);
    }
}
