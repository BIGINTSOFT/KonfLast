using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Dokuma.Infrastructure.Persistence.Configurations;

public sealed class DokumaOperatorConfiguration : IEntityTypeConfiguration<DokumaOperator>
{
    public void Configure(EntityTypeBuilder<DokumaOperator> builder)
    {
        builder.ToTable("Operators", t => t.HasCheckConstraint("CK_Operators_Role", UpperSnakeEnumConverter.CheckConstraintSql<DokumaOperatorRole>("Role")));
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

public sealed class DokumaOperatorPhotoConfiguration : IEntityTypeConfiguration<DokumaOperatorPhoto>
{
    public void Configure(EntityTypeBuilder<DokumaOperatorPhoto> builder)
    {
        builder.ToTable("OperatorPhotos");
        builder.HasKey(p => p.OperatorId);
        builder.Property(p => p.ContentType).HasMaxLength(30).IsRequired();
        builder.HasOne(p => p.Operator).WithOne().HasForeignKey<DokumaOperatorPhoto>(p => p.OperatorId);
    }
}

public sealed class DokumaOperatorLoomConfiguration : IEntityTypeConfiguration<DokumaOperatorLoom>
{
    public void Configure(EntityTypeBuilder<DokumaOperatorLoom> builder)
    {
        builder.ToTable("OperatorLooms");
        builder.HasKey(l => new { l.OperatorId, l.LoomId });
        builder.HasOne(l => l.Operator).WithMany().HasForeignKey(l => l.OperatorId);
        builder.HasOne(l => l.Loom).WithMany().HasForeignKey(l => l.LoomId);
    }
}

public sealed class DokumaTabletConfiguration : IEntityTypeConfiguration<DokumaTablet>
{
    public void Configure(EntityTypeBuilder<DokumaTablet> builder)
    {
        builder.ToTable("Tablets");
        builder.Property(t => t.TabletCode).HasMaxLength(30).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(150);
        builder.Property(t => t.DeviceIdentifier).HasMaxLength(200);
        builder.Property(t => t.DeviceKeyHash).HasMaxLength(128);
        builder.Ignore(t => t.IsEnrolled);

        builder.HasOne(t => t.Loom).WithMany().HasForeignKey(t => t.LoomId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TabletCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
        builder.HasIndex(t => t.LoomId, "UX_Tablets_Loom").IsUnique().HasFilter($"[LoomId] IS NOT NULL AND {SoftDelete.ActiveRowsFilter}");
        builder.HasIndex(t => t.DeviceIdentifier, "UX_Tablets_Device").IsUnique().HasFilter($"[DeviceIdentifier] IS NOT NULL AND {SoftDelete.ActiveRowsFilter}");
    }
}

public sealed class DokumaTabletSessionConfiguration : IEntityTypeConfiguration<DokumaTabletSession>
{
    public void Configure(EntityTypeBuilder<DokumaTabletSession> builder)
    {
        builder.ToTable("TabletSessions", t => t.HasCheckConstraint("CK_TabletSessions_EndReason", UpperSnakeEnumConverter.CheckConstraintSql<DokumaSessionEndReason>("EndReason")));
        builder.HasOne(s => s.Tablet).WithMany().HasForeignKey(s => s.TabletId);
        builder.HasOne(s => s.Operator).WithMany().HasForeignKey(s => s.OperatorId);

        // Tablette ve personelde aynı anda tek açık oturum olur.
        builder.HasIndex(s => s.TabletId, "UX_TabletSessions_Open").IsUnique().HasFilter("[EndedAt] IS NULL");
        builder.HasIndex(s => s.OperatorId, "UX_TabletSessions_OperatorOpen").IsUnique().HasFilter("[EndedAt] IS NULL");
    }
}

public sealed class DokumaSessionPauseConfiguration : IEntityTypeConfiguration<DokumaSessionPause>
{
    public void Configure(EntityTypeBuilder<DokumaSessionPause> builder)
    {
        builder.ToTable("SessionPauses", t => t.HasCheckConstraint("CK_SessionPauses_Kind", UpperSnakeEnumConverter.CheckConstraintSql<DokumaPauseKind>("Kind")));
        builder.Property(p => p.Note).HasMaxLength(500);
        builder.HasOne(p => p.Session).WithMany(s => s.Pauses).HasForeignKey(p => p.SessionId);
        builder.HasOne(p => p.Reason).WithMany().HasForeignKey(p => p.ReasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.BreakSchedule).WithMany().HasForeignKey(p => p.BreakScheduleId).OnDelete(DeleteBehavior.Restrict);

        // Oturumda aynı anda tek açık duraklama olur.
        builder.HasIndex(p => p.SessionId, "UX_SessionPauses_Open").IsUnique().HasFilter("[EndedAt] IS NULL");
    }
}

public sealed class DokumaRfidCardConfiguration : IEntityTypeConfiguration<DokumaRfidCard>
{
    public void Configure(EntityTypeBuilder<DokumaRfidCard> builder)
    {
        builder.ToTable("RfidCards");
        builder.Property(c => c.Uid).HasMaxLength(10).IsRequired();
        builder.Property(c => c.Label).HasMaxLength(50);
        builder.HasIndex(c => c.Uid).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class DokumaStopReasonConfiguration : IEntityTypeConfiguration<DokumaStopReason>
{
    public void Configure(EntityTypeBuilder<DokumaStopReason> builder)
    {
        builder.ToTable("StopReasons", t => t.HasCheckConstraint("CK_StopReasons_Category", UpperSnakeEnumConverter.CheckConstraintSql<DokumaStopCategory>("Category")));
        builder.Property(r => r.Code).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => r.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class DokumaLoomStopConfiguration : IEntityTypeConfiguration<DokumaLoomStop>
{
    public void Configure(EntityTypeBuilder<DokumaLoomStop> builder)
    {
        builder.ToTable("LoomStops", t => t.HasCheckConstraint("CK_LoomStops_Category", UpperSnakeEnumConverter.CheckConstraintSql<DokumaStopCategory>("Category")));
        builder.Property(s => s.Note).HasMaxLength(500);
        builder.Property(s => s.StartedBy).HasMaxLength(150).IsRequired();
        builder.Property(s => s.EndedBy).HasMaxLength(150);
        builder.HasOne(s => s.Loom).WithMany().HasForeignKey(s => s.LoomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Beam).WithMany().HasForeignKey(s => s.BeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Reason).WithMany().HasForeignKey(s => s.ReasonId).OnDelete(DeleteBehavior.Restrict);

        // Tezgâhta aynı anda tek açık duruş olur.
        builder.HasIndex(s => s.LoomId, "UX_LoomStops_Open").IsUnique().HasFilter("[EndedAt] IS NULL");
        builder.HasIndex(s => new { s.LoomId, s.StartedAt });
    }
}
