using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class WorkSessionConfiguration : IEntityTypeConfiguration<WorkSession>
{
    public void Configure(EntityTypeBuilder<WorkSession> builder)
    {
        builder.ToTable("WorkSessions", t =>
        {
            t.HasCheckConstraint(
                "CK_WorkSessions_Status", UpperSnakeEnumConverter.CheckConstraintSql<WorkSessionStatus>("Status"));
            t.HasCheckConstraint(
                "CK_WorkSessions_EndReason",
                "[EndReason] IS NULL OR " + UpperSnakeEnumConverter.CheckConstraintSql<WorkSessionEndReason>("EndReason"));
            t.HasCheckConstraint("CK_WorkSessions_Period", "[LogoutAt] IS NULL OR [LogoutAt] >= [LoginAt]");
        });

        builder.Property(s => s.ClosedBy).HasMaxLength(100);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasOne(s => s.Operator).WithMany().HasForeignKey(s => s.OperatorId);
        builder.HasOne(s => s.Machine).WithMany().HasForeignKey(s => s.MachineId);
        builder.HasOne(s => s.Tablet).WithMany().HasForeignKey(s => s.TabletId);

        // Bir operatörün ve bir makinenin aynı anda tek açık oturumu.
        builder.HasIndex(s => s.OperatorId)
            .IsUnique()
            .HasDatabaseName("UX_WorkSessions_OpenPerOperator")
            .HasFilter("[LogoutAt] IS NULL");
        builder.HasIndex(s => s.MachineId)
            .IsUnique()
            .HasDatabaseName("UX_WorkSessions_OpenPerMachine")
            .HasFilter("[LogoutAt] IS NULL");

        // Operatör / makine bazlı süre ve performans raporları; tarih aralığı listeleri.
        builder.HasIndex(s => new { s.OperatorId, s.LoginAt });
        builder.HasIndex(s => new { s.MachineId, s.LoginAt });
        builder.HasIndex(s => s.LoginAt);
        builder.HasIndex(s => s.TabletId);
    }
}

public sealed class OperatorOperationQualificationConfiguration : IEntityTypeConfiguration<OperatorOperationQualification>
{
    public void Configure(EntityTypeBuilder<OperatorOperationQualification> builder)
    {
        builder.ToTable("OperatorOperationQualifications");
        builder.HasKey(q => new { q.OperatorId, q.OperationId });

        builder.HasOne(q => q.Operator).WithMany().HasForeignKey(q => q.OperatorId);
        builder.HasOne(q => q.Operation).WithMany().HasForeignKey(q => q.OperationId);

        builder.HasIndex(q => q.OperationId);
    }
}

public sealed class OperatorMachineConfiguration : IEntityTypeConfiguration<OperatorMachine>
{
    public void Configure(EntityTypeBuilder<OperatorMachine> builder)
    {
        builder.ToTable("OperatorMachines");
        builder.HasKey(m => new { m.OperatorId, m.MachineId });

        builder.HasOne(m => m.Operator).WithMany().HasForeignKey(m => m.OperatorId);
        builder.HasOne(m => m.Machine).WithMany().HasForeignKey(m => m.MachineId);

        builder.HasIndex(m => m.MachineId);
    }
}

public sealed class OperatorPhotoConfiguration : IEntityTypeConfiguration<OperatorPhoto>
{
    public void Configure(EntityTypeBuilder<OperatorPhoto> builder)
    {
        builder.ToTable("OperatorPhotos");
        builder.HasKey(p => p.OperatorId);

        builder.Property(p => p.OperatorId).ValueGeneratedNever();
        builder.Property(p => p.Content).IsRequired();
        builder.Property(p => p.ContentType).HasMaxLength(30).IsUnicode(false).IsRequired();

        builder.HasOne(p => p.Operator).WithOne().HasForeignKey<OperatorPhoto>(p => p.OperatorId);
    }
}
