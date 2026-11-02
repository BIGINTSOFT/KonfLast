using ERP.Core.Infrastructure.Persistence.Converters;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Orme.Infrastructure.Persistence.Configurations;

public sealed class OrmeKnitOrderConfiguration : IEntityTypeConfiguration<OrmeKnitOrder>
{
    public void Configure(EntityTypeBuilder<OrmeKnitOrder> builder)
    {
        builder.ToTable("KnitOrders", t =>
        {
            t.HasCheckConstraint("CK_KnitOrders_Status", UpperSnakeEnumConverter.CheckConstraintSql<KnitOrderStatus>("Status"));
            t.HasCheckConstraint("CK_KnitOrders_PlannedKilograms", "[PlannedKilograms] > 0");
            t.HasCheckConstraint("CK_KnitOrders_PlanSequence", "[PlanSequence] IS NULL OR [PlanSequence] >= 1");
        });

        builder.Property(k => k.KnitOrderNo).HasMaxLength(30).IsRequired();
        builder.Property(k => k.PlannedKilograms).HasPrecision(12, 2);
        builder.Property(k => k.Notes).HasMaxLength(1000);

        builder.HasOne(k => k.OrderLine).WithMany().HasForeignKey(k => k.OrderLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(k => k.PlannedMachine).WithMany().HasForeignKey(k => k.PlannedMachineId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(k => k.KnitOrderNo).IsUnique();
        builder.HasIndex(k => new { k.PlannedMachineId, k.PlanSequence }).HasFilter("[PlannedMachineId] IS NOT NULL");
    }
}

public sealed class OrmeKnitOrderMachineConfiguration : IEntityTypeConfiguration<OrmeKnitOrderMachine>
{
    public void Configure(EntityTypeBuilder<OrmeKnitOrderMachine> builder)
    {
        builder.ToTable("KnitOrderMachines", t =>
            t.HasCheckConstraint("CK_KnitOrderMachines_Released", "[ReleasedOn] IS NULL OR [ReleasedOn] >= [AssignedOn]"));

        builder.HasOne(m => m.KnitOrder).WithMany(k => k.Machines).HasForeignKey(m => m.KnitOrderId);
        builder.HasOne(m => m.Machine).WithMany().HasForeignKey(m => m.MachineId).OnDelete(DeleteBehavior.Restrict);

        // Bir makinede aynı anda tek emir çalışır.
        builder.HasIndex(m => m.MachineId, "UX_KnitOrderMachines_RunningMachine").IsUnique().HasFilter("[ReleasedOn] IS NULL");
    }
}

public sealed class OrmeYarnIssueConfiguration : IEntityTypeConfiguration<OrmeYarnIssue>
{
    public void Configure(EntityTypeBuilder<OrmeYarnIssue> builder)
    {
        builder.ToTable("YarnIssues", t => t.HasCheckConstraint("CK_YarnIssues_Kilograms", "[Kilograms] > 0"));

        builder.Property(i => i.Kilograms).HasPrecision(12, 2);

        builder.HasOne(i => i.KnitOrder).WithMany(k => k.Issues).HasForeignKey(i => i.KnitOrderId);
        builder.HasOne(i => i.YarnLot).WithMany(l => l.Issues).HasForeignKey(i => i.YarnLotId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrmeYarnTakeBackConfiguration : IEntityTypeConfiguration<OrmeYarnTakeBack>
{
    public void Configure(EntityTypeBuilder<OrmeYarnTakeBack> builder)
    {
        builder.ToTable("YarnTakeBacks", t => t.HasCheckConstraint("CK_YarnTakeBacks_Kilograms", "[Kilograms] > 0"));

        builder.Property(t => t.Kilograms).HasPrecision(12, 2);
        builder.Property(t => t.Notes).HasMaxLength(500);

        builder.HasOne(t => t.KnitOrder).WithMany(k => k.TakeBacks).HasForeignKey(t => t.KnitOrderId);
        builder.HasOne(t => t.YarnLot).WithMany(l => l.TakeBacks).HasForeignKey(t => t.YarnLotId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrmeFabricRollConfiguration : IEntityTypeConfiguration<OrmeFabricRoll>
{
    public void Configure(EntityTypeBuilder<OrmeFabricRoll> builder)
    {
        builder.ToTable("FabricRolls", t =>
        {
            t.HasCheckConstraint("CK_FabricRolls_Status", UpperSnakeEnumConverter.CheckConstraintSql<FabricRollStatus>("Status"));
            t.HasCheckConstraint("CK_FabricRolls_Kilograms", "[Kilograms] > 0");
            t.HasCheckConstraint("CK_FabricRolls_Meters", "[Meters] IS NULL OR [Meters] > 0");
            t.HasCheckConstraint(
                "CK_FabricRolls_CalculatedStatus", $"[CalculatedStatus] IS NULL OR {UpperSnakeEnumConverter.CheckConstraintSql<FabricRollStatus>("CalculatedStatus")}");
            t.HasCheckConstraint("CK_FabricRolls_InspectedMeters", "[InspectedMeters] IS NULL OR [InspectedMeters] > 0");
            t.HasCheckConstraint("CK_FabricRolls_InspectedWidthCm", "[InspectedWidthCm] IS NULL OR [InspectedWidthCm] > 0");
            t.HasCheckConstraint("CK_FabricRolls_InspectedWeightGsm", "[InspectedWeightGsm] IS NULL OR [InspectedWeightGsm] > 0");
        });

        builder.Property(r => r.RollNo).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Kilograms).HasPrecision(12, 2);
        builder.Property(r => r.Meters).HasPrecision(12, 2);
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.Property(r => r.InspectedMeters).HasPrecision(12, 2);
        builder.Property(r => r.InspectedWidthCm).HasPrecision(6, 2);
        builder.Property(r => r.InspectedWeightGsm).HasPrecision(7, 2);
        builder.Property(r => r.SpecDeviation).HasMaxLength(200);
        builder.Property(r => r.PointsPer100M2).HasPrecision(8, 2);
        builder.Property(r => r.OverrideNote).HasMaxLength(500);
        builder.Property(r => r.InspectionNotes).HasMaxLength(500);

        builder.HasOne(r => r.KnitOrder).WithMany(k => k.Rolls).HasForeignKey(r => r.KnitOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Machine).WithMany().HasForeignKey(r => r.MachineId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.RollNo).IsUnique();
        builder.HasOne(r => r.CutByOperator).WithMany().HasForeignKey(r => r.CutByOperatorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.ClientRequestId, "UX_FabricRolls_ClientRequestId").IsUnique().HasFilter("[ClientRequestId] IS NOT NULL");
        builder.HasIndex(r => r.Status);
    }
}
