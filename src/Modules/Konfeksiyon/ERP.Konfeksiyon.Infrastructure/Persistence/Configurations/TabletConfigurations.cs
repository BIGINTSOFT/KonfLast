using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ERP.Core.Infrastructure.Persistence.Configurations;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class TabletConfiguration : IEntityTypeConfiguration<Tablet>
{
    public void Configure(EntityTypeBuilder<Tablet> builder)
    {
        builder.ToTable("Tablets");

        builder.Property(t => t.TabletCode).HasMaxLength(30).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(150);
        builder.Property(t => t.DeviceIdentifier).HasMaxLength(200);
        builder.Property(t => t.DeviceKeyHash).HasMaxLength(64).IsUnicode(false);

        builder.HasOne(t => t.Machine).WithMany().HasForeignKey(t => t.MachineId);

        builder.HasIndex(t => t.TabletCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);

        // Bir makineye tek tablet; bir fiziksel cihaz tek tablet kaydına bağlanır.
        builder.HasIndex(t => t.MachineId)
            .IsUnique()
            .HasDatabaseName("UX_Tablets_Machine")
            .HasFilter("[MachineId] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(t => t.DeviceIdentifier)
            .IsUnique()
            .HasDatabaseName("UX_Tablets_DeviceIdentifier")
            .HasFilter("[DeviceIdentifier] IS NOT NULL AND [IsDeleted] = 0");
    }
}

public sealed class MachineStatusLogConfiguration : IEntityTypeConfiguration<MachineStatusLog>
{
    public void Configure(EntityTypeBuilder<MachineStatusLog> builder)
    {
        builder.ToTable("MachineStatusLogs", t =>
        {
            t.HasCheckConstraint(
                "CK_MachineStatusLogs_Status", UpperSnakeEnumConverter.CheckConstraintSql<MachineStatus>("Status"));
            t.HasCheckConstraint("CK_MachineStatusLogs_Period", "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]");
        });

        builder.Property(l => l.Reason).HasMaxLength(300);
        builder.Property(l => l.ChangedBy).HasMaxLength(100).IsRequired();

        builder.HasOne(l => l.Machine).WithMany().HasForeignKey(l => l.MachineId);

        // Makine başına en fazla bir açık durum kaydı.
        builder.HasIndex(l => l.MachineId)
            .IsUnique()
            .HasDatabaseName("UX_MachineStatusLogs_OpenPerMachine")
            .HasFilter("[EndedAt] IS NULL");

        // Duruş raporları makine + zaman aralığı ve durum + zaman aralığı ile sorgulanır.
        builder.HasIndex(l => new { l.MachineId, l.StartedAt });
        builder.HasIndex(l => new { l.Status, l.StartedAt });
    }
}
