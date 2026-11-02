using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Dokuma.Infrastructure.Persistence.Configurations;

public sealed class DokumaWarpOrderConfiguration : IEntityTypeConfiguration<DokumaWarpOrder>
{
    public void Configure(EntityTypeBuilder<DokumaWarpOrder> builder)
    {
        builder.ToTable("WarpOrders", t =>
        {
            t.HasCheckConstraint("CK_WarpOrders_Status", UpperSnakeEnumConverter.CheckConstraintSql<WarpOrderStatus>("Status"));
            t.HasCheckConstraint("CK_WarpOrders_PlannedBeamCount", "[PlannedBeamCount] >= 1");
            t.HasCheckConstraint("CK_WarpOrders_MetersPerBeam", "[MetersPerBeam] > 0");
            t.HasCheckConstraint("CK_WarpOrders_Ends", "[Ends] >= 1");
        });

        builder.Property(w => w.WarpNo).HasMaxLength(30).IsRequired();
        builder.Property(w => w.MetersPerBeam).HasPrecision(12, 2);
        builder.Property(w => w.Notes).HasMaxLength(1000);

        builder.HasOne(w => w.OrderLine).WithMany().HasForeignKey(w => w.OrderLineId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => w.WarpNo).IsUnique();
    }
}

public sealed class DokumaWarpIssueConfiguration : IEntityTypeConfiguration<DokumaWarpIssue>
{
    public void Configure(EntityTypeBuilder<DokumaWarpIssue> builder)
    {
        builder.ToTable("WarpIssues", t => t.HasCheckConstraint("CK_WarpIssues_Kilograms", "[Kilograms] > 0"));

        builder.Property(i => i.Kilograms).HasPrecision(12, 2);

        builder.HasOne(i => i.WarpOrder).WithMany(w => w.Issues).HasForeignKey(i => i.WarpOrderId);
        builder.HasOne(i => i.YarnLot).WithMany(l => l.Issues).HasForeignKey(i => i.YarnLotId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DokumaBeamConfiguration : IEntityTypeConfiguration<DokumaBeam>
{
    public void Configure(EntityTypeBuilder<DokumaBeam> builder)
    {
        builder.ToTable("Beams", t =>
        {
            t.HasCheckConstraint("CK_Beams_SizingMode", UpperSnakeEnumConverter.CheckConstraintSql<SizingMode>("SizingMode"));
            t.HasCheckConstraint("CK_Beams_Status", UpperSnakeEnumConverter.CheckConstraintSql<BeamStatus>("Status"));
            t.HasCheckConstraint(
                "CK_Beams_DrawingMethod", $"[DrawingMethod] IS NULL OR {UpperSnakeEnumConverter.CheckConstraintSql<DrawingMethod>("DrawingMethod")}");
            t.HasCheckConstraint("CK_Beams_Meters", "[Meters] > 0");
            t.HasCheckConstraint("CK_Beams_Ends", "[Ends] >= 1");
            t.HasCheckConstraint("CK_Beams_LeftoverMeters", "[LeftoverMeters] IS NULL OR [LeftoverMeters] >= 0");
            t.HasCheckConstraint("CK_Beams_SizingPickUpPercent", "[SizingPickUpPercent] IS NULL OR [SizingPickUpPercent] BETWEEN 0 AND 500");
            t.HasCheckConstraint("CK_Beams_SizingViscositySeconds", "[SizingViscositySeconds] IS NULL OR [SizingViscositySeconds] > 0");
            t.HasCheckConstraint("CK_Beams_PlanSequence", "[PlanSequence] IS NULL OR [PlanSequence] >= 1");
        });

        builder.Property(b => b.BeamNo).HasMaxLength(30).IsRequired();
        builder.Property(b => b.SizingPickUpPercent).HasPrecision(5, 2);
        builder.Property(b => b.SizingViscositySeconds).HasPrecision(6, 1);
        builder.HasOne(b => b.SizingRecipe).WithMany().HasForeignKey(b => b.SizingRecipeId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(b => b.Meters).HasPrecision(12, 2);
        builder.Property(b => b.Notes).HasMaxLength(500);

        builder.HasOne(b => b.WarpOrder).WithMany(w => w.Beams).HasForeignKey(b => b.WarpOrderId);
        builder.HasOne(b => b.SizingSupplier).WithMany().HasForeignKey(b => b.SizingSupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Loom).WithMany().HasForeignKey(b => b.LoomId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(b => b.LeftoverMeters).HasPrecision(12, 2);

        // Kapasite planı: leventin bağlanacağı tezgâh ve kuyruktaki sırası.
        builder.HasOne(b => b.PlannedLoom).WithMany().HasForeignKey(b => b.PlannedLoomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(b => new { b.PlannedLoomId, b.PlanSequence });

        // Bir tezgâhta aynı anda tek levent bağlı olur.
        builder.HasIndex(b => b.LoomId, "UX_Beams_LoomOnLoom").IsUnique().HasFilter("[Status] = 'ON_LOOM'");

        builder.HasIndex(b => b.BeamNo).IsUnique();
        builder.HasIndex(b => b.Status);
    }
}

public sealed class DokumaWeftIssueConfiguration : IEntityTypeConfiguration<DokumaWeftIssue>
{
    public void Configure(EntityTypeBuilder<DokumaWeftIssue> builder)
    {
        builder.ToTable("WeftIssues", t => t.HasCheckConstraint("CK_WeftIssues_Kilograms", "[Kilograms] > 0"));

        builder.Property(i => i.Kilograms).HasPrecision(12, 2);

        builder.HasOne(i => i.Beam).WithMany(b => b.WeftIssues).HasForeignKey(i => i.BeamId);
        builder.HasOne(i => i.YarnLot).WithMany(l => l.WeftIssues).HasForeignKey(i => i.YarnLotId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DokumaFabricRollConfiguration : IEntityTypeConfiguration<DokumaFabricRoll>
{
    public void Configure(EntityTypeBuilder<DokumaFabricRoll> builder)
    {
        builder.ToTable("FabricRolls", t =>
        {
            t.HasCheckConstraint("CK_FabricRolls_Status", UpperSnakeEnumConverter.CheckConstraintSql<FabricRollStatus>("Status"));
            t.HasCheckConstraint("CK_FabricRolls_Meters", "[Meters] > 0");
            t.HasCheckConstraint("CK_FabricRolls_Kilograms", "[Kilograms] IS NULL OR [Kilograms] > 0");
            t.HasCheckConstraint(
                "CK_FabricRolls_CalculatedStatus", $"[CalculatedStatus] IS NULL OR {UpperSnakeEnumConverter.CheckConstraintSql<FabricRollStatus>("CalculatedStatus")}");
            t.HasCheckConstraint("CK_FabricRolls_InspectedMeters", "[InspectedMeters] IS NULL OR [InspectedMeters] > 0");
            t.HasCheckConstraint("CK_FabricRolls_InspectedWidthCm", "[InspectedWidthCm] IS NULL OR [InspectedWidthCm] > 0");
        });

        builder.Property(r => r.InspectedMeters).HasPrecision(12, 2);
        builder.Property(r => r.InspectedWidthCm).HasPrecision(6, 2);
        builder.Property(r => r.PointsPer100M2).HasPrecision(8, 2);
        builder.Property(r => r.OverrideNote).HasMaxLength(500);
        builder.Property(r => r.InspectionNotes).HasMaxLength(500);

        builder.Property(r => r.RollNo).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Meters).HasPrecision(12, 2);
        builder.Property(r => r.Kilograms).HasPrecision(12, 2);
        builder.Property(r => r.Notes).HasMaxLength(500);

        builder.HasOne(r => r.Beam).WithMany(b => b.Rolls).HasForeignKey(r => r.BeamId);
        builder.HasOne(r => r.Loom).WithMany().HasForeignKey(r => r.LoomId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.RollNo).IsUnique();
        builder.HasOne(r => r.CutByOperator).WithMany().HasForeignKey(r => r.CutByOperatorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.ClientRequestId, "UX_FabricRolls_ClientRequest").IsUnique().HasFilter("[ClientRequestId] IS NOT NULL");
        builder.HasIndex(r => r.Status);
    }
}

public sealed class DokumaRollDefectConfiguration : IEntityTypeConfiguration<DokumaRollDefect>
{
    public void Configure(EntityTypeBuilder<DokumaRollDefect> builder)
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

public sealed class DokumaDefectTypeConfiguration : IEntityTypeConfiguration<DokumaDefectType>
{
    public void Configure(EntityTypeBuilder<DokumaDefectType> builder)
    {
        builder.ToTable("DefectTypes");
        builder.Property(d => d.Code).HasMaxLength(30).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(d => d.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class DokumaQualitySettingsConfiguration : IEntityTypeConfiguration<DokumaQualitySettings>
{
    public void Configure(EntityTypeBuilder<DokumaQualitySettings> builder)
    {
        builder.ToTable("QualitySettings", t =>
        {
            t.HasCheckConstraint("CK_QualitySettings_Limits", "[FirstQualityMaxPoints] >= 0 AND [SecondQualityMaxPoints] >= [FirstQualityMaxPoints]");
        });
        builder.Property(s => s.FirstQualityMaxPoints).HasPrecision(8, 2);
        builder.Property(s => s.SecondQualityMaxPoints).HasPrecision(8, 2);
    }
}
