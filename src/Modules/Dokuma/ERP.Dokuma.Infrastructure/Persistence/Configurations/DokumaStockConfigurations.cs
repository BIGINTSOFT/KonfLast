using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Dokuma.Infrastructure.Persistence.Configurations;

public sealed class DokumaMaterialConfiguration : IEntityTypeConfiguration<DokumaMaterial>
{
    public void Configure(EntityTypeBuilder<DokumaMaterial> builder)
    {
        builder.ToTable("Materials", t =>
        {
            t.HasCheckConstraint("CK_Materials_Type", UpperSnakeEnumConverter.CheckConstraintSql<MaterialType>("Type"));
            t.HasCheckConstraint("CK_Materials_Unit", UpperSnakeEnumConverter.CheckConstraintSql<MaterialUnit>("Unit"));
            t.HasCheckConstraint("CK_Materials_MinimumStock", "[MinimumStock] IS NULL OR [MinimumStock] >= 0");
        });

        builder.Property(m => m.Code).HasMaxLength(30).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.MinimumStock).HasPrecision(14, 3);
        builder.Property(m => m.Notes).HasMaxLength(500);

        builder.HasOne(m => m.PreferredSupplier).WithMany().HasForeignKey(m => m.PreferredSupplierId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class DokumaMaterialLotConfiguration : IEntityTypeConfiguration<DokumaMaterialLot>
{
    public void Configure(EntityTypeBuilder<DokumaMaterialLot> builder)
    {
        builder.ToTable("MaterialLots", t =>
        {
            t.HasCheckConstraint("CK_MaterialLots_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_MaterialLots_RemainingQuantity", "[RemainingQuantity] >= 0");
            t.HasCheckConstraint("CK_MaterialLots_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
        });

        builder.Property(l => l.LotNo).HasMaxLength(30).IsRequired();
        builder.Property(l => l.SupplierLotNo).HasMaxLength(50);
        builder.Property(l => l.DespatchNo).HasMaxLength(50);
        builder.Property(l => l.Quantity).HasPrecision(14, 3);
        builder.Property(l => l.RemainingQuantity).HasPrecision(14, 3);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.Currency).HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.HasOne(l => l.Material).WithMany().HasForeignKey(l => l.MaterialId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Supplier).WithMany().HasForeignKey(l => l.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.PurchaseOrderLine).WithMany().HasForeignKey(l => l.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.LotNo).IsUnique();
        builder.HasIndex(l => new { l.MaterialId, l.ReceiptDate });
    }
}

public sealed class DokumaMaterialMovementConfiguration : IEntityTypeConfiguration<DokumaMaterialMovement>
{
    public void Configure(EntityTypeBuilder<DokumaMaterialMovement> builder)
    {
        builder.ToTable("MaterialMovements", t =>
        {
            t.HasCheckConstraint("CK_MaterialMovements_Type", UpperSnakeEnumConverter.CheckConstraintSql<MaterialMovementType>("Type"));
            t.HasCheckConstraint("CK_MaterialMovements_Quantity", "[Quantity] <> 0");
        });

        builder.Property(m => m.Quantity).HasPrecision(14, 3);
        builder.Property(m => m.Department).HasMaxLength(100);
        builder.Property(m => m.Reason).HasMaxLength(200);
        builder.Property(m => m.Reference).HasMaxLength(50);

        builder.HasOne(m => m.Material).WithMany().HasForeignKey(m => m.MaterialId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Lot).WithMany(l => l.Movements).HasForeignKey(m => m.LotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Loom).WithMany().HasForeignKey(m => m.LoomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Beam).WithMany().HasForeignKey(m => m.BeamId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.MaterialId, m.MovementDate });
        builder.HasIndex(m => m.BeamId).HasFilter("[BeamId] IS NOT NULL");
    }
}

public sealed class DokumaSizingRecipeConfiguration : IEntityTypeConfiguration<DokumaSizingRecipe>
{
    public void Configure(EntityTypeBuilder<DokumaSizingRecipe> builder)
    {
        builder.ToTable("SizingRecipes", t =>
        {
            t.HasCheckConstraint("CK_SizingRecipes_ConcentrationPercent", "[ConcentrationPercent] IS NULL OR [ConcentrationPercent] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_SizingRecipes_TargetPickUpPercent", "[TargetPickUpPercent] IS NULL OR [TargetPickUpPercent] BETWEEN 0 AND 500");
            t.HasCheckConstraint("CK_SizingRecipes_ViscositySeconds", "[ViscositySeconds] IS NULL OR [ViscositySeconds] > 0");
            t.HasCheckConstraint("CK_SizingRecipes_MachineSpeed", "[MachineSpeedMetersPerMinute] IS NULL OR [MachineSpeedMetersPerMinute] > 0");
        });

        builder.Property(r => r.Code).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.ConcentrationPercent).HasPrecision(5, 2);
        builder.Property(r => r.BathTemperatureC).HasPrecision(5, 1);
        builder.Property(r => r.ViscositySeconds).HasPrecision(6, 1);
        builder.Property(r => r.MachineSpeedMetersPerMinute).HasPrecision(6, 1);
        builder.Property(r => r.TargetPickUpPercent).HasPrecision(5, 2);
        builder.Property(r => r.Notes).HasMaxLength(500);

        builder.HasOne(r => r.Article).WithMany().HasForeignKey(r => r.ArticleId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);

        // Bir artikelin tek varsayılan reçetesi olur.
        builder.HasIndex(r => r.ArticleId, "UX_SizingRecipes_ArticleDefault").IsUnique().HasFilter("[IsDefault] = 1 AND [IsDeleted] = 0");
    }
}

public sealed class DokumaSizingRecipeLineConfiguration : IEntityTypeConfiguration<DokumaSizingRecipeLine>
{
    public void Configure(EntityTypeBuilder<DokumaSizingRecipeLine> builder)
    {
        builder.ToTable("SizingRecipeLines", t =>
        {
            t.HasCheckConstraint("CK_SizingRecipeLines_Sequence", "[Sequence] >= 1");
            t.HasCheckConstraint("CK_SizingRecipeLines_BathPercent", "[BathPercent] > 0 AND [BathPercent] <= 100");
        });

        builder.Property(l => l.BathPercent).HasPrecision(6, 3);

        builder.HasOne(l => l.Recipe).WithMany(r => r.Lines).HasForeignKey(l => l.RecipeId);
        builder.HasOne(l => l.Material).WithMany().HasForeignKey(l => l.MaterialId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.RecipeId, l.Sequence }).IsUnique();
    }
}
