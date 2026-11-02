using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class CuttingPlanConfiguration : IEntityTypeConfiguration<CuttingPlan>
{
    public void Configure(EntityTypeBuilder<CuttingPlan> builder)
    {
        builder.ToTable("CuttingPlans", t =>
        {
            t.HasCheckConstraint("CK_CuttingPlans_Status", UpperSnakeEnumConverter.CheckConstraintSql<CuttingPlanStatus>("Status"));
            t.HasCheckConstraint("CK_CuttingPlans_MarkerLength", "[MarkerLength] > 0");
            t.HasCheckConstraint("CK_CuttingPlans_EndAllowance", "[EndAllowance] >= 0");
            t.HasCheckConstraint("CK_CuttingPlans_PlannedPlies", "[PlannedPlies] > 0");
            t.HasCheckConstraint("CK_CuttingPlans_ActualPlies", "[ActualPlies] IS NULL OR [ActualPlies] > 0");
        });

        builder.Property(p => p.PlanNo).HasMaxLength(30).IsRequired();
        builder.Property(p => p.MarkerLength).HasPrecision(9, 3);
        builder.Property(p => p.EndAllowance).HasPrecision(9, 3);
        builder.Property(p => p.Notes).HasMaxLength(500);
        builder.Property(p => p.CutBy).HasMaxLength(100);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasOne(p => p.ProductionOrder).WithMany().HasForeignKey(p => p.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Color).WithMany().HasForeignKey(p => p.ColorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.PlanNo).IsUnique();

        // Siparişin kesim özeti (kesilen adetler) ve sipariş / durum süzgeçli liste bu index ile okunur.
        builder.HasIndex(p => new { p.ProductionOrderId, p.Status });
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.ColorId);
    }
}

public sealed class CuttingPlanSizeConfiguration : IEntityTypeConfiguration<CuttingPlanSize>
{
    public void Configure(EntityTypeBuilder<CuttingPlanSize> builder)
    {
        builder.ToTable("CuttingPlanSizes", t =>
        {
            t.HasCheckConstraint("CK_CuttingPlanSizes_Ratio", "[Ratio] > 0");
            t.HasCheckConstraint("CK_CuttingPlanSizes_CutQuantity", "[CutQuantity] IS NULL OR [CutQuantity] >= 0");
        });

        builder.HasOne(s => s.CuttingPlan).WithMany(p => p.Sizes).HasForeignKey(s => s.CuttingPlanId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Size).WithMany().HasForeignKey(s => s.SizeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.CuttingPlanId, s.SizeId }).IsUnique();
        builder.HasIndex(s => s.SizeId);
    }
}

public sealed class CuttingPlanLotConfiguration : IEntityTypeConfiguration<CuttingPlanLot>
{
    public void Configure(EntityTypeBuilder<CuttingPlanLot> builder)
    {
        builder.ToTable(
            "CuttingPlanLots",
            t => t.HasCheckConstraint("CK_CuttingPlanLots_UsedQuantity", "[UsedQuantity] IS NULL OR [UsedQuantity] >= 0"));

        builder.Property(l => l.UsedQuantity).HasPrecision(18, 3);

        builder.HasOne(l => l.CuttingPlan).WithMany(p => p.Lots).HasForeignKey(l => l.CuttingPlanId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.StockLot).WithMany().HasForeignKey(l => l.StockLotId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.CuttingPlanId, l.StockLotId }).IsUnique();
        builder.HasIndex(l => l.StockLotId);
    }
}
