using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneOperatorConfiguration : IEntityTypeConfiguration<BoyahaneOperator>
{
    public void Configure(EntityTypeBuilder<BoyahaneOperator> builder)
    {
        builder.ToTable("Operators", t => t.HasCheckConstraint("CK_Operators_Role", UpperSnakeEnumConverter.CheckConstraintSql<BoyahaneOperatorRole>("Role")));
        builder.Property(o => o.Code).HasMaxLength(30).IsRequired();
        builder.Property(o => o.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(o => o.LastName).HasMaxLength(100).IsRequired();
        builder.Property(o => o.Name).HasMaxLength(201).IsRequired();
        builder.Property(o => o.CardUid).HasMaxLength(10);

        builder.HasIndex(o => o.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
        builder.HasOne(o => o.BreakGroup).WithMany().HasForeignKey(o => o.BreakGroupId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(o => o.CardUid, "UX_Operators_CardUid").IsUnique().HasFilter($"[CardUid] IS NOT NULL AND {SoftDelete.ActiveRowsFilter}");
    }
}

public sealed class BoyahaneOperatorPhotoConfiguration : IEntityTypeConfiguration<BoyahaneOperatorPhoto>
{
    public void Configure(EntityTypeBuilder<BoyahaneOperatorPhoto> builder)
    {
        builder.ToTable("OperatorPhotos");
        builder.HasKey(p => p.OperatorId);
        builder.Property(p => p.ContentType).HasMaxLength(30).IsRequired();
        builder.HasOne(p => p.Operator).WithOne().HasForeignKey<BoyahaneOperatorPhoto>(p => p.OperatorId);
    }
}

public sealed class BoyahaneOperatorOperationConfiguration : IEntityTypeConfiguration<BoyahaneOperatorOperation>
{
    public void Configure(EntityTypeBuilder<BoyahaneOperatorOperation> builder)
    {
        builder.ToTable("OperatorOperations");
        builder.HasKey(q => new { q.OperatorId, q.OperationId });
        builder.HasOne(q => q.Operator).WithMany().HasForeignKey(q => q.OperatorId);
        builder.HasOne(q => q.Operation).WithMany().HasForeignKey(q => q.OperationId);
    }
}

public sealed class BoyahaneOperatorMachineConfiguration : IEntityTypeConfiguration<BoyahaneOperatorMachine>
{
    public void Configure(EntityTypeBuilder<BoyahaneOperatorMachine> builder)
    {
        builder.ToTable("OperatorMachines");
        builder.HasKey(m => new { m.OperatorId, m.MachineId });
        builder.HasOne(m => m.Operator).WithMany().HasForeignKey(m => m.OperatorId);
        builder.HasOne(m => m.Machine).WithMany().HasForeignKey(m => m.MachineId);
    }
}

public sealed class BoyahaneTabletConfiguration : IEntityTypeConfiguration<BoyahaneTablet>
{
    public void Configure(EntityTypeBuilder<BoyahaneTablet> builder)
    {
        builder.ToTable("Tablets");
        builder.Property(t => t.TabletCode).HasMaxLength(30).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(150);
        builder.Property(t => t.DeviceIdentifier).HasMaxLength(200);
        builder.Property(t => t.DeviceKeyHash).HasMaxLength(128);
        builder.Ignore(t => t.IsEnrolled);

        builder.HasOne(t => t.Machine).WithMany().HasForeignKey(t => t.MachineId);

        builder.HasIndex(t => t.TabletCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
        builder.HasIndex(t => t.MachineId, "UX_Tablets_Machine").IsUnique().HasFilter($"[MachineId] IS NOT NULL AND {SoftDelete.ActiveRowsFilter}");
        builder.HasIndex(t => t.DeviceIdentifier, "UX_Tablets_Device").IsUnique().HasFilter($"[DeviceIdentifier] IS NOT NULL AND {SoftDelete.ActiveRowsFilter}");
    }
}

public sealed class BoyahaneTabletSessionConfiguration : IEntityTypeConfiguration<BoyahaneTabletSession>
{
    public void Configure(EntityTypeBuilder<BoyahaneTabletSession> builder)
    {
        builder.ToTable("TabletSessions", t => t.HasCheckConstraint("CK_TabletSessions_EndReason", UpperSnakeEnumConverter.CheckConstraintSql<BoyahaneSessionEndReason>("EndReason")));
        builder.HasOne(s => s.Tablet).WithMany().HasForeignKey(s => s.TabletId);
        builder.HasOne(s => s.Operator).WithMany().HasForeignKey(s => s.OperatorId);

        // Tablette ve personelde aynı anda tek açık oturum olur.
        builder.HasIndex(s => s.TabletId, "UX_TabletSessions_Open").IsUnique().HasFilter("[EndedAt] IS NULL");
        builder.HasIndex(s => s.OperatorId, "UX_TabletSessions_OperatorOpen").IsUnique().HasFilter("[EndedAt] IS NULL");
    }
}

public sealed class BoyahaneBatchStepConfiguration : IEntityTypeConfiguration<BoyahaneBatchStep>
{
    public void Configure(EntityTypeBuilder<BoyahaneBatchStep> builder)
    {
        builder.ToTable("BatchSteps", t =>
        {
            t.HasCheckConstraint("CK_BatchSteps_Status", UpperSnakeEnumConverter.CheckConstraintSql<BatchStepStatus>("Status"));
            t.HasCheckConstraint("CK_BatchSteps_ReworkKind", UpperSnakeEnumConverter.CheckConstraintSql<ColorCheckResult>("ReworkKind"));
            t.HasCheckConstraint("CK_BatchSteps_ColorResult", UpperSnakeEnumConverter.CheckConstraintSql<ColorCheckResult>("ColorResult"));
        });

        builder.Property(s => s.Note).HasMaxLength(500);

        builder.HasOne(s => s.Batch).WithMany(b => b.Steps).HasForeignKey(s => s.BatchId);
        builder.HasOne(s => s.Operation).WithMany().HasForeignKey(s => s.OperationId);
        builder.HasOne(s => s.Machine).WithMany().HasForeignKey(s => s.MachineId);
        builder.HasOne(s => s.Tablet).WithMany().HasForeignKey(s => s.TabletId);
        builder.HasOne(s => s.StartedByOperator).WithMany().HasForeignKey(s => s.StartedByOperatorId);
        builder.HasOne(s => s.CompletedByOperator).WithMany().HasForeignKey(s => s.CompletedByOperatorId);
        builder.HasOne(s => s.StrippingRecipe).WithMany().HasForeignKey(s => s.StrippingRecipeId);

        builder.HasIndex(s => new { s.BatchId, s.Sequence });

        // Partide ve makinede aynı anda tek adım işlemde olur.
        builder.HasIndex(s => s.BatchId, "UX_BatchSteps_BatchInProgress").IsUnique().HasFilter("[Status] = 'IN_PROGRESS'");
        builder.HasIndex(s => s.MachineId, "UX_BatchSteps_MachineInProgress").IsUnique().HasFilter("[Status] = 'IN_PROGRESS' AND [MachineId] IS NOT NULL");
    }
}

public sealed class BoyahaneBatchConsumptionConfiguration : IEntityTypeConfiguration<BoyahaneBatchConsumption>
{
    public void Configure(EntityTypeBuilder<BoyahaneBatchConsumption> builder)
    {
        builder.ToTable("BatchConsumptions", t =>
        {
            t.HasCheckConstraint("CK_BatchConsumptions_Actual", "[ActualKilograms] >= 0");
        });

        builder.Property(c => c.PlannedKilograms).HasPrecision(12, 3);
        builder.Property(c => c.ActualKilograms).HasPrecision(12, 3);

        builder.HasOne(c => c.Step).WithMany(s => s.Consumptions).HasForeignKey(c => c.StepId);
        builder.HasOne(c => c.DyeChemical).WithMany().HasForeignKey(c => c.DyeChemicalId);
    }
}

public sealed class BoyahaneStepRequestConfiguration : IEntityTypeConfiguration<BoyahaneStepRequest>
{
    public void Configure(EntityTypeBuilder<BoyahaneStepRequest> builder)
    {
        builder.ToTable("StepRequests");
        builder.HasIndex(r => r.RequestId).IsUnique();
    }
}

public sealed class BoyahaneRfidCardConfiguration : IEntityTypeConfiguration<BoyahaneRfidCard>
{
    public void Configure(EntityTypeBuilder<BoyahaneRfidCard> builder)
    {
        builder.ToTable("RfidCards", t => t.HasCheckConstraint("CK_RfidCards_CardType", UpperSnakeEnumConverter.CheckConstraintSql<BoyahaneCardType>("CardType")));
        builder.Property(c => c.Uid).HasMaxLength(10).IsRequired();
        builder.Property(c => c.Label).HasMaxLength(50);
        builder.HasIndex(c => c.Uid).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class BoyahaneStopReasonConfiguration : IEntityTypeConfiguration<BoyahaneStopReason>
{
    public void Configure(EntityTypeBuilder<BoyahaneStopReason> builder)
    {
        builder.ToTable("StopReasons", t => t.HasCheckConstraint("CK_StopReasons_Category", UpperSnakeEnumConverter.CheckConstraintSql<BoyahaneStopReasonCategory>("Category")));
        builder.Property(r => r.Code).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => r.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class BoyahaneSessionPauseConfiguration : IEntityTypeConfiguration<BoyahaneSessionPause>
{
    public void Configure(EntityTypeBuilder<BoyahaneSessionPause> builder)
    {
        builder.ToTable("SessionPauses", t => t.HasCheckConstraint("CK_SessionPauses_Kind", UpperSnakeEnumConverter.CheckConstraintSql<BoyahanePauseKind>("Kind")));
        builder.Property(p => p.Note).HasMaxLength(500);
        builder.HasOne(p => p.Session).WithMany(s => s.Pauses).HasForeignKey(p => p.SessionId);
        builder.HasOne(p => p.Reason).WithMany().HasForeignKey(p => p.ReasonId);
        builder.HasOne(p => p.BreakSchedule).WithMany().HasForeignKey(p => p.BreakScheduleId).OnDelete(DeleteBehavior.Restrict);

        // Oturumda aynı anda tek açık duraklama olur.
        builder.HasIndex(p => p.SessionId, "UX_SessionPauses_Open").IsUnique().HasFilter("[EndedAt] IS NULL");
    }
}

public sealed class BoyahaneMachineStatusLogConfiguration : IEntityTypeConfiguration<BoyahaneMachineStatusLog>
{
    public void Configure(EntityTypeBuilder<BoyahaneMachineStatusLog> builder)
    {
        builder.ToTable("MachineStatusLogs", t => t.HasCheckConstraint("CK_MachineStatusLogs_Status", UpperSnakeEnumConverter.CheckConstraintSql<DyeMachineStatus>("Status")));
        builder.Property(l => l.Reason).HasMaxLength(500);
        builder.Property(l => l.ChangedBy).HasMaxLength(100).IsRequired();
        builder.HasOne(l => l.Machine).WithMany().HasForeignKey(l => l.MachineId);
        builder.HasIndex(l => new { l.MachineId, l.StartedAt });
    }
}
