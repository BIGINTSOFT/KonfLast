using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Orme.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Orme.Infrastructure.Persistence.Configurations;

public sealed class OrmeShiftConfiguration : IEntityTypeConfiguration<OrmeShift>
{
    public void Configure(EntityTypeBuilder<OrmeShift> builder)
    {
        builder.ToTable("Shifts", t => t.HasCheckConstraint("CK_Shifts_Times", "[StartTime] <> [EndTime]"));
        builder.Property(s => s.Code).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.StartTime).HasColumnType("time(0)");
        builder.Property(s => s.EndTime).HasColumnType("time(0)");
        builder.HasIndex(s => s.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class OrmeBreakGroupConfiguration : IEntityTypeConfiguration<OrmeBreakGroup>
{
    public void Configure(EntityTypeBuilder<OrmeBreakGroup> builder)
    {
        builder.ToTable("BreakGroups");
        builder.Property(g => g.Code).HasMaxLength(30).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(g => g.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class OrmeBreakScheduleConfiguration : IEntityTypeConfiguration<OrmeBreakSchedule>
{
    public void Configure(EntityTypeBuilder<OrmeBreakSchedule> builder)
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
