using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ERP.Core.Infrastructure.Persistence.Configurations;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class MachineTypeConfiguration : IEntityTypeConfiguration<MachineType>
{
    public void Configure(EntityTypeBuilder<MachineType> builder)
    {
        builder.ToTable("MachineTypes");

        builder.Property(t => t.Code).HasMaxLength(30).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(t => t.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class OperationConfiguration : IEntityTypeConfiguration<Operation>
{
    public void Configure(EntityTypeBuilder<Operation> builder)
    {
        builder.ToTable("Operations", t => t.HasCheckConstraint("CK_Operations_Sam", "[Sam] >= 0"));

        builder.Property(o => o.OperationCode).HasMaxLength(30).IsRequired();
        builder.Property(o => o.Name).HasMaxLength(150).IsRequired();
        builder.Property(o => o.Description).HasMaxLength(500);
        builder.Property(o => o.Sam).HasPrecision(9, 4);

        builder.HasOne(o => o.MachineType).WithMany().HasForeignKey(o => o.MachineTypeId);

        builder.HasIndex(o => o.OperationCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class MachineConfiguration : IEntityTypeConfiguration<Machine>
{
    public void Configure(EntityTypeBuilder<Machine> builder)
    {
        builder.ToTable("Machines", t =>
        {
            t.HasCheckConstraint("CK_Machines_Status", UpperSnakeEnumConverter.CheckConstraintSql<MachineStatus>("Status"));
            t.HasCheckConstraint("CK_Machines_MaintenanceInterval", "[MaintenanceIntervalDays] IS NULL OR [MaintenanceIntervalDays] > 0");
            t.HasCheckConstraint("CK_Machines_Warranty", "[WarrantyEndDate] IS NULL OR [PurchaseDate] IS NULL OR [WarrantyEndDate] >= [PurchaseDate]");
        });

        builder.Property(m => m.MachineCode).HasMaxLength(30).IsRequired();
        builder.Property(m => m.MachineName).HasMaxLength(150).IsRequired();
        builder.Property(m => m.Brand).HasMaxLength(100);
        builder.Property(m => m.ModelNo).HasMaxLength(100);
        builder.Property(m => m.SerialNo).HasMaxLength(100);
        builder.Property(m => m.AssetNo).HasMaxLength(50);
        builder.Property(m => m.Notes).HasMaxLength(500);

        builder.HasOne(m => m.MachineType).WithMany().HasForeignKey(m => m.MachineTypeId);
        builder.HasOne(m => m.Operation).WithMany().HasForeignKey(m => m.OperationId);
        builder.HasOne(m => m.Supplier).WithMany().HasForeignKey(m => m.SupplierId);
        builder.HasIndex(m => m.SupplierId);

        builder.HasIndex(m => m.MachineCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
        builder.HasIndex(m => m.Status);
    }
}

public sealed class MachineOperationConfiguration : IEntityTypeConfiguration<MachineOperation>
{
    public void Configure(EntityTypeBuilder<MachineOperation> builder)
    {
        builder.ToTable("MachineOperations");
        builder.HasKey(o => new { o.MachineId, o.OperationId });

        builder.HasOne(o => o.Machine).WithMany().HasForeignKey(o => o.MachineId);
        builder.HasOne(o => o.Operation).WithMany().HasForeignKey(o => o.OperationId);

        builder.HasIndex(o => o.OperationId);
    }
}

public sealed class OperatorConfiguration : IEntityTypeConfiguration<Operator>
{
    public void Configure(EntityTypeBuilder<Operator> builder)
    {
        builder.ToTable("Operators", t => t.HasCheckConstraint(
            "CK_Operators_Role", UpperSnakeEnumConverter.CheckConstraintSql<StaffRole>("Role")));

        builder.Property(o => o.OperatorCode).HasMaxLength(30).IsRequired();
        builder.Property(o => o.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(o => o.LastName).HasMaxLength(100).IsRequired();

        builder.HasOne(o => o.BreakGroup).WithMany().HasForeignKey(o => o.BreakGroupId);

        builder.HasIndex(o => o.OperatorCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
        builder.HasIndex(o => o.BreakGroupId);
    }
}
