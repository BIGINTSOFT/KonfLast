using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ERP.Core.Infrastructure.Persistence.Configurations;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts", t => t.HasCheckConstraint("CK_Shifts_Times", "[StartTime] <> [EndTime]"));

        builder.Property(s => s.Code).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.StartTime).HasColumnType("time(0)");
        builder.Property(s => s.EndTime).HasColumnType("time(0)");

        builder.HasIndex(s => s.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class BreakGroupConfiguration : IEntityTypeConfiguration<BreakGroup>
{
    public void Configure(EntityTypeBuilder<BreakGroup> builder)
    {
        builder.ToTable("BreakGroups");

        builder.Property(g => g.Code).HasMaxLength(30).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(g => g.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class BreakScheduleConfiguration : IEntityTypeConfiguration<BreakSchedule>
{
    public void Configure(EntityTypeBuilder<BreakSchedule> builder)
    {
        builder.ToTable("BreakSchedules", t =>
        {
            t.HasCheckConstraint(
                "CK_BreakSchedules_Policy", UpperSnakeEnumConverter.CheckConstraintSql<BreakPolicy>("Policy"));
            t.HasCheckConstraint("CK_BreakSchedules_Times", "[StartTime] <> [EndTime]");
        });

        builder.Property(b => b.Code).HasMaxLength(30).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
        builder.Property(b => b.StartTime).HasColumnType("time(0)");
        builder.Property(b => b.EndTime).HasColumnType("time(0)");

        builder.HasOne(b => b.Shift).WithMany().HasForeignKey(b => b.ShiftId);
        builder.HasOne(b => b.BreakGroup).WithMany().HasForeignKey(b => b.BreakGroupId);

        builder.HasIndex(b => b.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
        builder.HasIndex(b => b.ShiftId);
        builder.HasIndex(b => b.BreakGroupId);
    }
}

public sealed class WorkSessionPauseConfiguration : IEntityTypeConfiguration<WorkSessionPause>
{
    public void Configure(EntityTypeBuilder<WorkSessionPause> builder)
    {
        builder.ToTable("WorkSessionPauses", t =>
        {
            t.HasCheckConstraint(
                "CK_WorkSessionPauses_Type", UpperSnakeEnumConverter.CheckConstraintSql<WorkSessionPauseType>("Type"));
            t.HasCheckConstraint("CK_WorkSessionPauses_Period", "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]");
        });

        builder.Property(p => p.Note).HasMaxLength(200);

        builder.HasOne(p => p.WorkSession).WithMany().HasForeignKey(p => p.WorkSessionId);
        builder.HasOne(p => p.BreakSchedule).WithMany().HasForeignKey(p => p.BreakScheduleId);
        builder.HasOne(p => p.StopReason).WithMany().HasForeignKey(p => p.StopReasonId);

        // Bir oturumda aynı anda tek duraklama sürer.
        builder.HasIndex(p => p.WorkSessionId)
            .IsUnique()
            .HasDatabaseName("UX_WorkSessionPauses_OpenPerSession")
            .HasFilter("[EndedAt] IS NULL");

        // Oturumun duraklama dökümü; mola planının o gün kullanılıp kullanılmadığı; tür bazlı süre raporları.
        builder.HasIndex(p => new { p.WorkSessionId, p.StartedAt });
        builder.HasIndex(p => new { p.BreakScheduleId, p.StartedAt });
        builder.HasIndex(p => new { p.Type, p.StartedAt });
    }
}

public sealed class StopReasonConfiguration : IEntityTypeConfiguration<StopReason>
{
    /// <summary>Hazır gelen duruş nedenleri. Id'ler sabittir; yenileri kullanıcı tarafından eklenir.</summary>
    private static readonly (int Id, string Code, string Name, StopReasonCategory Category, int SortOrder)[] Defaults =
    [
        (1, "SU", "Su", StopReasonCategory.Pause, 10),
        (2, "TUVALET", "Tuvalet", StopReasonCategory.Pause, 20),
        (3, "KISISEL", "Kişisel ihtiyaç", StopReasonCategory.Pause, 30),
        (4, "BEKLEME", "Bekleme", StopReasonCategory.Pause, 40),
        (5, "DIGER", "Diğer", StopReasonCategory.Pause, 90),
        (6, "IGNE", "İğne kırıldı", StopReasonCategory.Breakdown, 10),
        (7, "IPLIK", "İplik / masura sorunu", StopReasonCategory.Breakdown, 20),
        (8, "MOTOR", "Motor arızası", StopReasonCategory.Breakdown, 30),
        (9, "ELEKTRIK", "Elektrik arızası", StopReasonCategory.Breakdown, 40),
        (10, "ARIZA-DIGER", "Diğer arıza", StopReasonCategory.Breakdown, 90),
    ];

    public void Configure(EntityTypeBuilder<StopReason> builder)
    {
        builder.ToTable("StopReasons", t => t.HasCheckConstraint(
            "CK_StopReasons_Category", UpperSnakeEnumConverter.CheckConstraintSql<StopReasonCategory>("Category")));

        builder.Property(r => r.Code).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(r => r.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);

        // Tabletin seçenek listesi: tür + sıra.
        builder.HasIndex(r => new { r.Category, r.SortOrder });

        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(Defaults.Select(r => new StopReason
        {
            Id = r.Id, Code = r.Code, Name = r.Name, Category = r.Category, SortOrder = r.SortOrder, IsActive = true,
            CreatedAt = seededAt, CreatedBy = "system",
        }));
    }
}
