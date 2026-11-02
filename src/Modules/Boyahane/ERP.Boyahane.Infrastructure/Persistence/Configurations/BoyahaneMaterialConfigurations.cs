using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneFabricConfiguration : IEntityTypeConfiguration<BoyahaneFabric>
{
    public void Configure(EntityTypeBuilder<BoyahaneFabric> builder)
    {
        builder.ToTable("Fabrics", t =>
        {
            t.HasCheckConstraint("CK_Fabrics_FabricType", UpperSnakeEnumConverter.CheckConstraintSql<FabricType>("FabricType"));
            t.HasCheckConstraint("CK_Fabrics_DefaultUnit", "[DefaultUnit] IN ('METER', 'KILOGRAM')");
            t.HasCheckConstraint("CK_Fabrics_RawWidthCm", "[RawWidthCm] > 0");
            t.HasCheckConstraint("CK_Fabrics_RawWeightGsm", "[RawWeightGsm] > 0");
            t.HasCheckConstraint(
                "CK_Fabrics_ShrinkageTolerancePercent",
                "[ShrinkageTolerancePercent] IS NULL OR [ShrinkageTolerancePercent] BETWEEN 0 AND 100");
        });

        builder.Property(f => f.FabricCode).HasMaxLength(30).IsRequired();
        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Structure).HasMaxLength(100);
        builder.Property(f => f.Composition).HasMaxLength(200);
        builder.Property(f => f.RawWidthCm).HasPrecision(6, 2);
        builder.Property(f => f.FinishedWidthCm).HasPrecision(6, 2);
        builder.Property(f => f.ShrinkageTolerancePercent).HasPrecision(5, 2);

        builder.HasIndex(f => f.FabricCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class BoyahaneYarnConfiguration : IEntityTypeConfiguration<BoyahaneYarn>
{
    public void Configure(EntityTypeBuilder<BoyahaneYarn> builder)
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

        builder.HasIndex(y => y.YarnCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}
