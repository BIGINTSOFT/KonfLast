using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneMachineConfiguration : IEntityTypeConfiguration<BoyahaneMachine>
{
    public void Configure(EntityTypeBuilder<BoyahaneMachine> builder)
    {
        builder.ToTable("Machines", t =>
        {
            t.HasCheckConstraint("CK_Machines_MachineType", UpperSnakeEnumConverter.CheckConstraintSql<DyeMachineType>("MachineType"));
            t.HasCheckConstraint("CK_Machines_CapacityKg", "[CapacityKg] > 0");
            t.HasCheckConstraint("CK_Machines_Status", UpperSnakeEnumConverter.CheckConstraintSql<DyeMachineStatus>("Status"));
        });

        builder.Property(m => m.Code).HasMaxLength(30).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.CapacityKg).HasPrecision(10, 2);
        builder.Property(m => m.DefaultLiquorRatio).HasPrecision(6, 2);

        builder.HasIndex(m => m.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class BoyahaneBatchConfiguration : IEntityTypeConfiguration<BoyahaneBatch>
{
    public void Configure(EntityTypeBuilder<BoyahaneBatch> builder)
    {
        builder.ToTable("Batches", t =>
        {
            t.HasCheckConstraint("CK_Batches_Status", UpperSnakeEnumConverter.CheckConstraintSql<BatchStatus>("Status"));
            t.HasCheckConstraint("CK_Batches_LiquorRatio", "[LiquorRatio] > 0");
        });

        builder.Property(b => b.BatchNo).HasMaxLength(30).IsRequired();
        builder.Property(b => b.LiquorRatio).HasPrecision(6, 2);
        builder.Property(b => b.Notes).HasMaxLength(1000);
        builder.Property(b => b.CapacityOverrideBy).HasMaxLength(100);
        builder.Property(b => b.CapacityOverrideNote).HasMaxLength(500);
        builder.Property(b => b.CardUid).HasMaxLength(10);

        builder.HasOne(b => b.Machine).WithMany().HasForeignKey(b => b.MachineId);
        builder.HasOne(b => b.RecipeTrial).WithMany().HasForeignKey(b => b.RecipeTrialId);
        builder.HasOne(b => b.ReworkOfBatch).WithMany().HasForeignKey(b => b.ReworkOfBatchId);
        builder.HasOne(b => b.Route).WithMany().HasForeignKey(b => b.RouteId);

        builder.HasIndex(b => b.BatchNo).IsUnique();
        builder.HasIndex(b => b.Status);

        // Bir kart aynı anda tek partide olur.
        builder.HasIndex(b => b.CardUid, "UX_Batches_CardUid").IsUnique().HasFilter("[CardUid] IS NOT NULL");
    }
}

public sealed class BoyahaneBatchItemConfiguration : IEntityTypeConfiguration<BoyahaneBatchItem>
{
    public void Configure(EntityTypeBuilder<BoyahaneBatchItem> builder)
    {
        builder.ToTable("BatchItems", t =>
        {
            t.HasCheckConstraint("CK_BatchItems_Item", "([RollId] IS NOT NULL AND [YarnLotId] IS NULL) OR ([RollId] IS NULL AND [YarnLotId] IS NOT NULL)");
            t.HasCheckConstraint("CK_BatchItems_Kilograms", "[Kilograms] > 0");
            t.HasCheckConstraint("CK_BatchItems_FinalResult", UpperSnakeEnumConverter.CheckConstraintSql<FinalQualityResult>("FinalResult"));
        });

        builder.Property(i => i.Kilograms).HasPrecision(12, 2);
        builder.Property(i => i.Meters).HasPrecision(12, 2);
        builder.Property(i => i.FinalNote).HasMaxLength(500);
        builder.Property(i => i.FinalOverrideBy).HasMaxLength(100);
        builder.Property(i => i.FinalOverrideNote).HasMaxLength(500);
        builder.Property(i => i.FinalKilograms).HasPrecision(12, 2);
        builder.Property(i => i.FinalMeters).HasPrecision(12, 2);

        builder.HasOne(i => i.Batch).WithMany(b => b.Items).HasForeignKey(i => i.BatchId);
        builder.HasOne(i => i.OrderLine).WithMany().HasForeignKey(i => i.OrderLineId);
        builder.HasOne(i => i.Roll).WithMany().HasForeignKey(i => i.RollId);
        builder.HasOne(i => i.YarnLot).WithMany().HasForeignKey(i => i.YarnLotId);

        // Bir mal aynı anda tek etkin partide olur.
        builder.HasIndex(i => i.RollId, "UX_BatchItems_ActiveRoll").IsUnique().HasFilter("[IsActive] = 1 AND [RollId] IS NOT NULL");
        builder.HasIndex(i => i.YarnLotId, "UX_BatchItems_ActiveYarnLot").IsUnique().HasFilter("[IsActive] = 1 AND [YarnLotId] IS NOT NULL");
    }
}
