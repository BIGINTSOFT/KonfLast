using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Orme.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Orme.Infrastructure.Persistence.Configurations;

public sealed class OrmeRollDefectConfiguration : IEntityTypeConfiguration<OrmeRollDefect>
{
    public void Configure(EntityTypeBuilder<OrmeRollDefect> builder)
    {
        builder.ToTable("RollDefects", t =>
        {
            t.HasCheckConstraint("CK_RollDefects_AtMeter", "[AtMeter] >= 0");
            t.HasCheckConstraint("CK_RollDefects_LengthCm", "[LengthCm] > 0");
            t.HasCheckConstraint("CK_RollDefects_Points", "[Points] BETWEEN 1 AND 4");
        });

        builder.Property(d => d.AtMeter).HasPrecision(12, 2);
        builder.Property(d => d.LengthCm).HasPrecision(8, 2);

        builder.HasOne(d => d.Roll).WithMany(r => r.Defects).HasForeignKey(d => d.RollId);
        builder.HasOne(d => d.DefectType).WithMany().HasForeignKey(d => d.DefectTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrmeDefectTypeConfiguration : IEntityTypeConfiguration<OrmeDefectType>
{
    public void Configure(EntityTypeBuilder<OrmeDefectType> builder)
    {
        builder.ToTable("DefectTypes");
        builder.Property(d => d.Code).HasMaxLength(30).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(d => d.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class OrmeQualitySettingsConfiguration : IEntityTypeConfiguration<OrmeQualitySettings>
{
    public void Configure(EntityTypeBuilder<OrmeQualitySettings> builder)
    {
        builder.ToTable("QualitySettings", t =>
        {
            t.HasCheckConstraint("CK_QualitySettings_Limits", "[FirstQualityMaxPoints] >= 0 AND [SecondQualityMaxPoints] >= [FirstQualityMaxPoints]");
            t.HasCheckConstraint("CK_QualitySettings_Tolerances", "[WidthTolerancePercent] BETWEEN 0 AND 100 AND [WeightTolerancePercent] BETWEEN 0 AND 100");
        });
        builder.Property(s => s.FirstQualityMaxPoints).HasPrecision(8, 2);
        builder.Property(s => s.SecondQualityMaxPoints).HasPrecision(8, 2);
        builder.Property(s => s.WidthTolerancePercent).HasPrecision(5, 2);
        builder.Property(s => s.WeightTolerancePercent).HasPrecision(5, 2);
    }
}
