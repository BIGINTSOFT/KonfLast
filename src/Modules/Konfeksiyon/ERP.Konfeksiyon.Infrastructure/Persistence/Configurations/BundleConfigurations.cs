using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class BundleConfiguration : IEntityTypeConfiguration<Bundle>
{
    public void Configure(EntityTypeBuilder<Bundle> builder)
    {
        builder.ToTable("Bundles", t =>
        {
            t.HasCheckConstraint("CK_Bundles_Status", UpperSnakeEnumConverter.CheckConstraintSql<BundleStatus>("Status"));
            t.HasCheckConstraint(
                "CK_Bundles_SequenceMode", UpperSnakeEnumConverter.CheckConstraintSql<OperationSequenceMode>("SequenceMode"));
            t.HasCheckConstraint("CK_Bundles_Quantity", "[Quantity] > 0");
        });

        builder.Property(b => b.BundleNo).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(b => b.RowVersion).IsRowVersion();

        builder.HasOne(b => b.ProductionOrder).WithMany().HasForeignKey(b => b.ProductionOrderId);
        builder.HasOne(b => b.Color).WithMany().HasForeignKey(b => b.ColorId);
        builder.HasOne(b => b.Size).WithMany().HasForeignKey(b => b.SizeId);
        builder.HasOne(b => b.ProductionRoute).WithMany().HasForeignKey(b => b.ProductionRouteId);
        builder.HasOne(b => b.CuttingPlan).WithMany().HasForeignKey(b => b.CuttingPlanId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.BundleNo).IsUnique();
        builder.HasIndex(b => b.CuttingPlanId);

        // Sipariş kalemi başına demetlenen adet ve siparişin demet listesi bu index ile hesaplanır.
        builder.HasIndex(b => new { b.ProductionOrderId, b.ColorId, b.SizeId, b.Status });
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.ColorId);
        builder.HasIndex(b => b.SizeId);
        builder.HasIndex(b => b.ProductionRouteId);
    }
}

public sealed class BundleOperationConfiguration : IEntityTypeConfiguration<BundleOperation>
{
    public void Configure(EntityTypeBuilder<BundleOperation> builder)
    {
        builder.ToTable("BundleOperations", t =>
        {
            t.HasCheckConstraint(
                "CK_BundleOperations_Status", UpperSnakeEnumConverter.CheckConstraintSql<BundleOperationStatus>("Status"));
            t.HasCheckConstraint(
                "CK_BundleOperations_Quantities",
                "[PlannedQuantity] > 0 AND [ProducedQuantity] >= 0 AND [RejectedQuantity] >= 0 " +
                "AND [ProducedQuantity] + [RejectedQuantity] <= [PlannedQuantity]");
            t.HasCheckConstraint("CK_BundleOperations_ReworkQuantity", "[ReworkQuantity] >= 0");
        });

        builder.Property(o => o.Sam).HasPrecision(9, 4);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasOne(o => o.Bundle).WithMany(b => b.Operations).HasForeignKey(o => o.BundleId);
        builder.HasOne(o => o.RouteOperation).WithMany().HasForeignKey(o => o.RouteOperationId);
        builder.HasOne(o => o.Operation).WithMany().HasForeignKey(o => o.OperationId);
        builder.HasOne(o => o.Machine).WithMany().HasForeignKey(o => o.MachineId);
        builder.HasOne(o => o.Operator).WithMany().HasForeignKey(o => o.OperatorId);

        builder.HasIndex(o => new { o.BundleId, o.RouteOperationId }).IsUnique();
        builder.HasIndex(o => new { o.BundleId, o.SequenceNo });

        // "Bu operasyon için hazır / işlemdeki demetler" ve makine / operatör bazlı raporlar.
        builder.HasIndex(o => new { o.OperationId, o.Status });
        builder.HasIndex(o => new { o.MachineId, o.StartedAt });
        builder.HasIndex(o => new { o.OperatorId, o.StartedAt });
        builder.HasIndex(o => o.RouteOperationId);

        // Eşzamanlılığın son güvencesi: bir demette, bir makinede ve bir operatörde aynı anda tek işlem.
        builder.HasIndex(o => o.BundleId)
            .IsUnique()
            .HasDatabaseName("UX_BundleOperations_InProgressPerBundle")
            .HasFilter("[Status] = 'IN_PROGRESS'");
        builder.HasIndex(o => o.MachineId)
            .IsUnique()
            .HasDatabaseName("UX_BundleOperations_InProgressPerMachine")
            .HasFilter("[Status] = 'IN_PROGRESS' AND [MachineId] IS NOT NULL");
        builder.HasIndex(o => o.OperatorId)
            .IsUnique()
            .HasDatabaseName("UX_BundleOperations_InProgressPerOperator")
            .HasFilter("[Status] = 'IN_PROGRESS' AND [OperatorId] IS NOT NULL");
    }
}

public sealed class BundleOperationEventConfiguration : IEntityTypeConfiguration<BundleOperationEvent>
{
    public const string AppendOnlyTrigger = "TR_BundleOperationEvents_AppendOnly";

    public void Configure(EntityTypeBuilder<BundleOperationEvent> builder)
    {
        builder.ToTable("BundleOperationEvents", t =>
        {
            t.HasCheckConstraint(
                "CK_BundleOperationEvents_EventType",
                UpperSnakeEnumConverter.CheckConstraintSql<BundleOperationEventType>("EventType"));
            t.HasCheckConstraint(
                "CK_BundleOperationEvents_Quantities", "[ProducedQuantity] >= 0 AND [RejectedQuantity] >= 0");

            // Kayıtlar değiştirilemez / silinemez; trigger migration'da oluşturulur.
            t.HasTrigger(AppendOnlyTrigger);
        });

        builder.HasOne(e => e.Bundle).WithMany().HasForeignKey(e => e.BundleId);
        builder.HasOne(e => e.BundleOperation).WithMany().HasForeignKey(e => e.BundleOperationId);
        builder.HasOne(e => e.Machine).WithMany().HasForeignKey(e => e.MachineId);
        builder.HasOne(e => e.Operator).WithMany().HasForeignKey(e => e.OperatorId);
        builder.HasOne(e => e.WorkSession).WithMany().HasForeignKey(e => e.WorkSessionId);
        builder.Property(e => e.EnteredBy).HasMaxLength(100);

        // Aynı istek yeniden gelirse olay ikinci kez yazılamaz.
        builder.HasIndex(e => new { e.RequestId, e.EventType }).IsUnique();

        // Tablo hızla büyür: demet geçmişi ile operatör / makine / tarih bazlı raporlar için.
        builder.HasIndex(e => new { e.BundleId, e.Id });
        builder.HasIndex(e => new { e.BundleOperationId, e.Id });
        builder.HasIndex(e => new { e.OperatorId, e.OccurredAt });
        builder.HasIndex(e => new { e.MachineId, e.OccurredAt });
        builder.HasIndex(e => new { e.OccurredAt, e.EventType });
        builder.HasIndex(e => e.WorkSessionId);
    }
}
