using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneOperationConfiguration : IEntityTypeConfiguration<BoyahaneOperation>
{
    public void Configure(EntityTypeBuilder<BoyahaneOperation> builder)
    {
        builder.ToTable("Operations", t => t.HasCheckConstraint("CK_Operations_Behavior", UpperSnakeEnumConverter.CheckConstraintSql<OperationBehavior>("Behavior")));
        builder.Property(o => o.Code).HasMaxLength(30).IsRequired();
        builder.Property(o => o.Name).HasMaxLength(100).IsRequired();
        builder.Property(o => o.Description).HasMaxLength(500);
        builder.Property(o => o.StandardMinutes).HasPrecision(10, 2);
        builder.HasIndex(o => o.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class BoyahaneMachineOperationConfiguration : IEntityTypeConfiguration<BoyahaneMachineOperation>
{
    public void Configure(EntityTypeBuilder<BoyahaneMachineOperation> builder)
    {
        builder.ToTable("MachineOperations");
        builder.HasKey(o => new { o.MachineId, o.OperationId });
        builder.HasOne(o => o.Machine).WithMany().HasForeignKey(o => o.MachineId);
        builder.HasOne(o => o.Operation).WithMany().HasForeignKey(o => o.OperationId);
    }
}

public sealed class BoyahaneRouteConfiguration : IEntityTypeConfiguration<BoyahaneRoute>
{
    public void Configure(EntityTypeBuilder<BoyahaneRoute> builder)
    {
        builder.ToTable("Routes", t => t.HasCheckConstraint("CK_Routes_ProcessType", UpperSnakeEnumConverter.CheckConstraintSql<OrderProcessType>("ProcessType")));
        builder.Property(r => r.Code).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => r.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);

        // İşlem türünün tek varsayılan rotası olur.
        builder.HasIndex(r => r.ProcessType, "UX_Routes_Default").IsUnique().HasFilter($"[IsDefault] = 1 AND {SoftDelete.ActiveRowsFilter}");
    }
}

public sealed class BoyahaneRouteStepConfiguration : IEntityTypeConfiguration<BoyahaneRouteStep>
{
    public void Configure(EntityTypeBuilder<BoyahaneRouteStep> builder)
    {
        builder.ToTable("RouteSteps");
        builder.HasOne(s => s.Route).WithMany(r => r.Steps).HasForeignKey(s => s.RouteId);
        builder.HasOne(s => s.Operation).WithMany().HasForeignKey(s => s.OperationId);
        builder.HasIndex(s => new { s.RouteId, s.Sequence }).IsUnique();
    }
}
