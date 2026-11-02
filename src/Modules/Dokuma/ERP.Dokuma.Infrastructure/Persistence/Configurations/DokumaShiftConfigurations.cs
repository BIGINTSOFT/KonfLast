using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Dokuma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Dokuma.Infrastructure.Persistence.Configurations;

public sealed class DokumaShiftConfiguration : IEntityTypeConfiguration<DokumaShift>
{
    public void Configure(EntityTypeBuilder<DokumaShift> builder)
    {
        builder.ToTable("Shifts", t => t.HasCheckConstraint("CK_Shifts_Times", "[StartTime] <> [EndTime]"));
        builder.Property(s => s.Code).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.StartTime).HasColumnType("time(0)");
        builder.Property(s => s.EndTime).HasColumnType("time(0)");
        builder.HasIndex(s => s.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class DokumaBreakGroupConfiguration : IEntityTypeConfiguration<DokumaBreakGroup>
{
    public void Configure(EntityTypeBuilder<DokumaBreakGroup> builder)
    {
        builder.ToTable("BreakGroups");
        builder.Property(g => g.Code).HasMaxLength(30).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(g => g.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class DokumaBreakScheduleConfiguration : IEntityTypeConfiguration<DokumaBreakSchedule>
{
    public void Configure(EntityTypeBuilder<DokumaBreakSchedule> builder)
    {
        builder.ToTable("BreakSchedules", t =>
        {
            t.HasCheckConstraint("CK_BreakSchedules_Times", "[StartTime] <> [EndTime]");
        });
        builder.Property(b => b.Code).HasMaxLength(30).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
        builder.Property(b => b.StartTime).HasColumnType("time(0)");
        builder.Property(b => b.EndTime).HasColumnType("time(0)");
        builder.HasOne(b => b.Shift).WithMany().HasForeignKey(b => b.ShiftId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.BreakGroup).WithMany().HasForeignKey(b => b.BreakGroupId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(b => b.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}
