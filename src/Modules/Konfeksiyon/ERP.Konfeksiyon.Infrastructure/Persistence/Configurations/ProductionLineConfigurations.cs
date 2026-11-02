using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ERP.Core.Infrastructure.Persistence.Configurations;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class FactoryConfiguration : IEntityTypeConfiguration<Factory>
{
    public void Configure(EntityTypeBuilder<Factory> builder)
    {
        builder.ToTable("Factories", t =>
        {
            t.HasCheckConstraint("CK_Factories_CountryCode", "LEN([CountryCode]) = 2");
            t.HasCheckConstraint("CK_Factories_IndoorArea", "[IndoorArea] IS NULL OR [IndoorArea] > 0");
        });
        builder.Property(f => f.Code).HasMaxLength(30).IsRequired();
        builder.Property(f => f.Name).HasMaxLength(150).IsRequired();
        builder.Property(f => f.AddressLine).HasMaxLength(500);
        builder.Property(f => f.District).HasMaxLength(100);
        builder.Property(f => f.City).HasMaxLength(100);
        builder.Property(f => f.PostalCode).HasMaxLength(10);
        builder.Property(f => f.CountryCode).HasColumnType("char(2)").IsRequired();
        builder.Property(f => f.Phone).HasMaxLength(30);
        builder.Property(f => f.Email).HasMaxLength(256);
        builder.Property(f => f.ManagerName).HasMaxLength(100);
        builder.Property(f => f.SgkWorkplaceNo).HasMaxLength(40);
        builder.HasIndex(f => f.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class ProductionAreaConfiguration : IEntityTypeConfiguration<ProductionArea>
{
    public void Configure(EntityTypeBuilder<ProductionArea> builder)
    {
        builder.ToTable("ProductionAreas");
        builder.Property(a => a.Code).HasMaxLength(30).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(150).IsRequired();
        builder.HasOne(a => a.Factory).WithMany().HasForeignKey(a => a.FactoryId);
        builder.HasIndex(a => a.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class ProductionLineConfiguration : IEntityTypeConfiguration<ProductionLine>
{
    public void Configure(EntityTypeBuilder<ProductionLine> builder)
    {
        builder.ToTable("ProductionLines");
        builder.Property(l => l.LineCode).HasMaxLength(30).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(150).IsRequired();
        builder.HasOne(l => l.ProductionArea).WithMany().HasForeignKey(l => l.ProductionAreaId);
        builder.HasIndex(l => l.LineCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class LineMachineAssignmentConfiguration : IEntityTypeConfiguration<LineMachineAssignment>
{
    public void Configure(EntityTypeBuilder<LineMachineAssignment> builder)
    {
        builder.ToTable("LineMachineAssignments", t =>
        {
            t.HasCheckConstraint("CK_LineMachineAssignments_Position", "[Position] > 0");
            t.HasCheckConstraint(
                "CK_LineMachineAssignments_Period", "[UnassignedAt] IS NULL OR [UnassignedAt] >= [AssignedAt]");
        });

        builder.HasOne(a => a.ProductionLine).WithMany().HasForeignKey(a => a.ProductionLineId);
        builder.HasOne(a => a.Machine).WithMany().HasForeignKey(a => a.MachineId);

        // Bir makine aynı anda tek bantta olabilir: eşzamanlı iki atamadan yalnızca biri başarılı olur.
        builder.HasIndex(a => a.MachineId)
            .IsUnique()
            .HasDatabaseName("UX_LineMachineAssignments_CurrentPerMachine")
            .HasFilter("[UnassignedAt] IS NULL");

        builder.HasIndex(a => new { a.ProductionLineId, a.UnassignedAt, a.Position });
        builder.HasIndex(a => new { a.MachineId, a.AssignedAt });
    }
}

public sealed class LineProductionPlanConfiguration : IEntityTypeConfiguration<LineProductionPlan>
{
    public void Configure(EntityTypeBuilder<LineProductionPlan> builder)
    {
        builder.ToTable("LineProductionPlans", t =>
        {
            t.HasCheckConstraint(
                "CK_LineProductionPlans_Status", UpperSnakeEnumConverter.CheckConstraintSql<LinePlanStatus>("Status"));
            t.HasCheckConstraint("CK_LineProductionPlans_Period", "[PlannedEnd] > [PlannedStart]");
            t.HasCheckConstraint("CK_LineProductionPlans_Quantity", "[PlannedQuantity] > 0");
        });

        builder.Property(p => p.Notes).HasMaxLength(500);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasOne(p => p.ProductionLine).WithMany().HasForeignKey(p => p.ProductionLineId);
        builder.HasOne(p => p.ProductionOrder).WithMany().HasForeignKey(p => p.ProductionOrderId);

        // "Bant şu an ne üretiyor" ve çakışma kontrolü bant + zaman aralığı ile sorgulanır.
        builder.HasIndex(p => new { p.ProductionLineId, p.PlannedStart, p.PlannedEnd });
        builder.HasIndex(p => new { p.ProductionOrderId, p.Status });
    }
}
