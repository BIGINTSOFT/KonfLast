using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Orme.Infrastructure.Persistence.Configurations;

public sealed class OrmeOperatorConfiguration : IEntityTypeConfiguration<OrmeOperator>
{
    public void Configure(EntityTypeBuilder<OrmeOperator> builder)
    {
        builder.ToTable("Operators", t => t.HasCheckConstraint("CK_Operators_Role", UpperSnakeEnumConverter.CheckConstraintSql<OrmeOperatorRole>("Role")));
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

public sealed class OrmeOperatorPhotoConfiguration : IEntityTypeConfiguration<OrmeOperatorPhoto>
{
    public void Configure(EntityTypeBuilder<OrmeOperatorPhoto> builder)
    {
        builder.ToTable("OperatorPhotos");
        builder.HasKey(p => p.OperatorId);
        builder.Property(p => p.ContentType).HasMaxLength(30).IsRequired();
        builder.HasOne(p => p.Operator).WithOne().HasForeignKey<OrmeOperatorPhoto>(p => p.OperatorId);
    }
}

public sealed class OrmeOperatorMachineConfiguration : IEntityTypeConfiguration<OrmeOperatorMachine>
{
    public void Configure(EntityTypeBuilder<OrmeOperatorMachine> builder)
    {
        builder.ToTable("OperatorMachines");
        builder.HasKey(l => new { l.OperatorId, l.MachineId });
        builder.HasOne(l => l.Operator).WithMany().HasForeignKey(l => l.OperatorId);
        builder.HasOne(l => l.Machine).WithMany().HasForeignKey(l => l.MachineId);
    }
}

public sealed class OrmeTabletConfiguration : IEntityTypeConfiguration<OrmeTablet>
{
    public void Configure(EntityTypeBuilder<OrmeTablet> builder)
    {
        builder.ToTable("Tablets");
        builder.Property(t => t.TabletCode).HasMaxLength(30).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(150);
        builder.Property(t => t.DeviceIdentifier).HasMaxLength(200);
        builder.Property(t => t.DeviceKeyHash).HasMaxLength(128);
        builder.Ignore(t => t.IsEnrolled);

        builder.HasOne(t => t.Machine).WithMany().HasForeignKey(t => t.MachineId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TabletCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
        builder.HasIndex(t => t.MachineId, "UX_Tablets_Machine").IsUnique().HasFilter($"[MachineId] IS NOT NULL AND {SoftDelete.ActiveRowsFilter}");
        builder.HasIndex(t => t.DeviceIdentifier, "UX_Tablets_Device").IsUnique().HasFilter($"[DeviceIdentifier] IS NOT NULL AND {SoftDelete.ActiveRowsFilter}");
    }
}

public sealed class OrmeTabletSessionConfiguration : IEntityTypeConfiguration<OrmeTabletSession>
{
    public void Configure(EntityTypeBuilder<OrmeTabletSession> builder)
    {
        builder.ToTable("TabletSessions", t => t.HasCheckConstraint("CK_TabletSessions_EndReason", UpperSnakeEnumConverter.CheckConstraintSql<OrmeSessionEndReason>("EndReason")));
        builder.HasOne(s => s.Tablet).WithMany().HasForeignKey(s => s.TabletId);
        builder.HasOne(s => s.Operator).WithMany().HasForeignKey(s => s.OperatorId);

        // Tablette ve personelde aynı anda tek açık oturum olur.
        builder.HasIndex(s => s.TabletId, "UX_TabletSessions_Open").IsUnique().HasFilter("[EndedAt] IS NULL");
        builder.HasIndex(s => s.OperatorId, "UX_TabletSessions_OperatorOpen").IsUnique().HasFilter("[EndedAt] IS NULL");
    }
}

public sealed class OrmeSessionPauseConfiguration : IEntityTypeConfiguration<OrmeSessionPause>
{
    public void Configure(EntityTypeBuilder<OrmeSessionPause> builder)
    {
        builder.ToTable("SessionPauses", t => t.HasCheckConstraint("CK_SessionPauses_Kind", UpperSnakeEnumConverter.CheckConstraintSql<OrmePauseKind>("Kind")));
        builder.Property(p => p.Note).HasMaxLength(500);
        builder.HasOne(p => p.Session).WithMany(s => s.Pauses).HasForeignKey(p => p.SessionId);
        builder.HasOne(p => p.Reason).WithMany().HasForeignKey(p => p.ReasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.BreakSchedule).WithMany().HasForeignKey(p => p.BreakScheduleId).OnDelete(DeleteBehavior.Restrict);

        // Oturumda aynı anda tek açık duraklama olur.
        builder.HasIndex(p => p.SessionId, "UX_SessionPauses_Open").IsUnique().HasFilter("[EndedAt] IS NULL");
    }
}

public sealed class OrmeRfidCardConfiguration : IEntityTypeConfiguration<OrmeRfidCard>
{
    public void Configure(EntityTypeBuilder<OrmeRfidCard> builder)
    {
        builder.ToTable("RfidCards");
        builder.Property(c => c.Uid).HasMaxLength(10).IsRequired();
        builder.Property(c => c.Label).HasMaxLength(50);
        builder.HasIndex(c => c.Uid).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class OrmeStopReasonConfiguration : IEntityTypeConfiguration<OrmeStopReason>
{
    public void Configure(EntityTypeBuilder<OrmeStopReason> builder)
    {
        builder.ToTable("StopReasons", t => t.HasCheckConstraint("CK_StopReasons_Category", UpperSnakeEnumConverter.CheckConstraintSql<OrmeStopCategory>("Category")));
        builder.Property(r => r.Code).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => r.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class OrmeMachineStopConfiguration : IEntityTypeConfiguration<OrmeMachineStop>
{
    public void Configure(EntityTypeBuilder<OrmeMachineStop> builder)
    {
        builder.ToTable("MachineStops", t => t.HasCheckConstraint("CK_MachineStops_Category", UpperSnakeEnumConverter.CheckConstraintSql<OrmeStopCategory>("Category")));
        builder.Property(s => s.Note).HasMaxLength(500);
        builder.Property(s => s.StartedBy).HasMaxLength(150).IsRequired();
        builder.Property(s => s.EndedBy).HasMaxLength(150);
        builder.HasOne(s => s.Machine).WithMany().HasForeignKey(s => s.MachineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.KnitOrder).WithMany().HasForeignKey(s => s.KnitOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Reason).WithMany().HasForeignKey(s => s.ReasonId).OnDelete(DeleteBehavior.Restrict);

        // Makinede aynı anda tek açık duruş olur.
        builder.HasIndex(s => s.MachineId, "UX_MachineStops_Open").IsUnique().HasFilter("[EndedAt] IS NULL");
        builder.HasIndex(s => new { s.MachineId, s.StartedAt });
    }
}
