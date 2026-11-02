using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Orme.Infrastructure.Persistence.Configurations;

public sealed class OrmeSupplierConfiguration : IEntityTypeConfiguration<OrmeSupplier>
{
    public void Configure(EntityTypeBuilder<OrmeSupplier> builder)
    {
        builder.ToTable("Suppliers", t => t.HasCheckConstraint("CK_Suppliers_LeadTimeDays", "[LeadTimeDays] IS NULL OR [LeadTimeDays] >= 0"));
        builder.Property(s => s.Code).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.TaxNumber).HasMaxLength(20);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(150);
        builder.Property(s => s.Currency).HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.HasIndex(s => s.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class OrmeYarnConfiguration : IEntityTypeConfiguration<OrmeYarn>
{
    public void Configure(EntityTypeBuilder<OrmeYarn> builder)
    {
        builder.ToTable("Yarns", t =>
        {
            t.HasCheckConstraint("CK_Yarns_CountSystem", UpperSnakeEnumConverter.CheckConstraintSql<YarnCountSystem>("CountSystem"));
            t.HasCheckConstraint("CK_Yarns_CountValue", "[CountValue] > 0");
            t.HasCheckConstraint("CK_Yarns_Plies", "[Plies] >= 1");
        });

        builder.Property(y => y.YarnCode).HasMaxLength(30).IsRequired();
        builder.Property(y => y.Name).HasMaxLength(200).IsRequired();
        builder.Property(y => y.CountValue).HasPrecision(8, 2);
        builder.Property(y => y.Composition).HasMaxLength(200);
        builder.Property(y => y.Twist).HasMaxLength(50);
        builder.Property(y => y.Color).HasMaxLength(100);

        builder.HasIndex(y => y.YarnCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class OrmeMachineConfiguration : IEntityTypeConfiguration<OrmeMachine>
{
    public void Configure(EntityTypeBuilder<OrmeMachine> builder)
    {
        builder.ToTable("Machines", t =>
        {
            t.HasCheckConstraint("CK_Machines_BedType", UpperSnakeEnumConverter.CheckConstraintSql<KnitBedType>("BedType"));
            t.HasCheckConstraint("CK_Machines_DiameterInch", "[DiameterInch] > 0");
            t.HasCheckConstraint("CK_Machines_Gauge", "[Gauge] > 0");
            t.HasCheckConstraint("CK_Machines_NeedleCount", "[NeedleCount] IS NULL OR [NeedleCount] > 0");
            t.HasCheckConstraint("CK_Machines_FeederCount", "[FeederCount] IS NULL OR [FeederCount] > 0");
            t.HasCheckConstraint("CK_Machines_NominalSpeedRpm", "[NominalSpeedRpm] IS NULL OR [NominalSpeedRpm] > 0");
        });

        builder.Property(m => m.Code).HasMaxLength(30).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Brand).HasMaxLength(100);

        builder.HasIndex(m => m.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class OrmeFabricConfiguration : IEntityTypeConfiguration<OrmeFabric>
{
    public void Configure(EntityTypeBuilder<OrmeFabric> builder)
    {
        builder.ToTable("Fabrics", t =>
        {
            t.HasCheckConstraint("CK_Fabrics_BedType", UpperSnakeEnumConverter.CheckConstraintSql<KnitBedType>("BedType"));
            t.HasCheckConstraint("CK_Fabrics_RawWeightGsm", "[RawWeightGsm] > 0");
            t.HasCheckConstraint("CK_Fabrics_RawWidthCm", "[RawWidthCm] IS NULL OR [RawWidthCm] > 0");
            t.HasCheckConstraint("CK_Fabrics_WastePercent", "[WastePercent] IS NULL OR [WastePercent] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_Fabrics_ExpectedKilogramsPerHour", "[ExpectedKilogramsPerHour] IS NULL OR [ExpectedKilogramsPerHour] > 0");
        });

        builder.Property(f => f.FabricCode).HasMaxLength(30).IsRequired();
        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.KnitName).HasMaxLength(100);
        builder.Property(f => f.Composition).HasMaxLength(200);
        builder.Property(f => f.RawWidthCm).HasPrecision(6, 2);
        builder.Property(f => f.WastePercent).HasPrecision(5, 2);
        builder.Property(f => f.ExpectedKilogramsPerHour).HasPrecision(8, 2);

        builder.HasIndex(f => f.FabricCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class OrmeFabricYarnConfiguration : IEntityTypeConfiguration<OrmeFabricYarn>
{
    public void Configure(EntityTypeBuilder<OrmeFabricYarn> builder)
    {
        builder.ToTable("FabricYarns", t =>
        {
            t.HasCheckConstraint("CK_FabricYarns_Sequence", "[Sequence] >= 1");
            t.HasCheckConstraint("CK_FabricYarns_Percent", "[Percent] > 0 AND [Percent] <= 100");
            t.HasCheckConstraint("CK_FabricYarns_StitchLengthMm", "[StitchLengthMm] IS NULL OR [StitchLengthMm] > 0");
        });

        builder.Property(y => y.Percent).HasPrecision(5, 2);
        builder.Property(y => y.StitchLengthMm).HasPrecision(5, 2);

        builder.HasOne(y => y.Fabric).WithMany(f => f.Yarns).HasForeignKey(y => y.FabricId);
        builder.HasOne(y => y.Yarn).WithMany().HasForeignKey(y => y.YarnId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(y => new { y.FabricId, y.Sequence }).IsUnique();
    }
}
