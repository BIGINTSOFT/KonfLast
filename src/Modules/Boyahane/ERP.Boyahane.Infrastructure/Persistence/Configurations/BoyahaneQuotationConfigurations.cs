using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneQuotationConfiguration : IEntityTypeConfiguration<BoyahaneQuotation>
{
    public void Configure(EntityTypeBuilder<BoyahaneQuotation> builder)
    {
        builder.ToTable("Quotations", t =>
        {
            t.HasCheckConstraint("CK_Quotations_Status", UpperSnakeEnumConverter.CheckConstraintSql<QuotationStatus>("Status"));
            t.HasCheckConstraint("CK_Quotations_Ownership", UpperSnakeEnumConverter.CheckConstraintSql<OrderOwnership>("Ownership"));
            t.HasCheckConstraint("CK_Quotations_DocumentLanguage", UpperSnakeEnumConverter.CheckConstraintSql<DocumentLanguage>("DocumentLanguage"));
            t.HasCheckConstraint("CK_Quotations_Party", "[CustomerId] IS NOT NULL OR [ProspectName] IS NOT NULL");
            t.HasCheckConstraint("CK_Quotations_ValidUntil", "[ValidUntil] >= [QuotationDate]");
        });

        builder.Property(q => q.QuotationNo).HasMaxLength(30).IsRequired();
        builder.Property(q => q.ProspectName).HasMaxLength(200);
        builder.Property(q => q.Currency).HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.Property(q => q.Notes).HasMaxLength(1000);
        builder.Property(q => q.DecidedBy).HasMaxLength(100);
        builder.Property(q => q.DecisionNote).HasMaxLength(500);

        builder.HasOne(q => q.Customer).WithMany().HasForeignKey(q => q.CustomerId);
        // Tekliften açılan taslak sipariş silinirse teklif kalır, bağı boşalır.
        builder.HasOne(q => q.Order).WithMany().HasForeignKey(q => q.OrderId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(q => q.QuotationNo).IsUnique();
        builder.HasIndex(q => q.QuotationDate);
        builder.HasIndex(q => q.Status);
        builder.HasIndex(q => q.OrderId);
    }
}

public sealed class BoyahaneQuotationLineConfiguration : IEntityTypeConfiguration<BoyahaneQuotationLine>
{
    public void Configure(EntityTypeBuilder<BoyahaneQuotationLine> builder)
    {
        builder.ToTable("QuotationLines", t =>
        {
            t.HasCheckConstraint("CK_QuotationLines_ProcessType", UpperSnakeEnumConverter.CheckConstraintSql<OrderProcessType>("ProcessType"));
            t.HasCheckConstraint("CK_QuotationLines_Unit", "[Unit] IN ('METER', 'KILOGRAM')");
            t.HasCheckConstraint("CK_QuotationLines_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_QuotationLines_Item", "([FabricId] IS NOT NULL AND [YarnId] IS NULL) OR ([FabricId] IS NULL AND [YarnId] IS NOT NULL)");
            t.HasCheckConstraint("CK_QuotationLines_MinutesSource", UpperSnakeEnumConverter.CheckConstraintSql<ProcessMinutesSource>("MinutesSource"));
            t.HasCheckConstraint("CK_QuotationLines_BatchKilograms", "[BatchKilograms] > 0");
        });

        builder.Property(l => l.ColorName).HasMaxLength(100);
        builder.Property(l => l.ColorCode).HasMaxLength(50);
        builder.Property(l => l.Quantity).HasPrecision(12, 2);
        builder.Property(l => l.BatchKilogramsInput).HasPrecision(10, 2);
        builder.Property(l => l.LiquorRatioInput).HasPrecision(6, 2);
        builder.Property(l => l.ProcessMinutesInput).HasPrecision(10, 2);
        builder.Property(l => l.FirePercentInput).HasPrecision(5, 2);
        builder.Property(l => l.OverheadPercentInput).HasPrecision(5, 2);
        builder.Property(l => l.MarginPercentInput).HasPrecision(7, 2);
        builder.Property(l => l.UnitPriceInput).HasPrecision(12, 4);
        builder.Property(l => l.BatchKilograms).HasPrecision(10, 2);
        builder.Property(l => l.LiquorRatio).HasPrecision(6, 2);
        builder.Property(l => l.ProcessMinutes).HasPrecision(10, 2);
        builder.Property(l => l.KilogramsPerMeter).HasPrecision(10, 4);
        builder.Property(l => l.ChemicalCost).HasPrecision(14, 4);
        builder.Property(l => l.PrintCost).HasPrecision(14, 4);
        builder.Property(l => l.MachineCost).HasPrecision(14, 4);
        builder.Property(l => l.LaborCost).HasPrecision(14, 4);
        builder.Property(l => l.FirePercent).HasPrecision(5, 2);
        builder.Property(l => l.FireCost).HasPrecision(14, 4);
        builder.Property(l => l.OverheadPercent).HasPrecision(5, 2);
        builder.Property(l => l.OverheadCost).HasPrecision(14, 4);
        builder.Property(l => l.UnitCost).HasPrecision(14, 4);
        builder.Property(l => l.MarginPercent).HasPrecision(9, 2);
        builder.Property(l => l.UnitPrice).HasPrecision(12, 4);
        builder.Property(l => l.CostWarning).HasMaxLength(500);
        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.HasOne(l => l.Quotation).WithMany(q => q.Lines).HasForeignKey(l => l.QuotationId);
        builder.HasOne(l => l.Fabric).WithMany().HasForeignKey(l => l.FabricId);
        builder.HasOne(l => l.Yarn).WithMany().HasForeignKey(l => l.YarnId);
        builder.HasOne(l => l.ColorCard).WithMany().HasForeignKey(l => l.ColorCardId);
        builder.HasOne(l => l.Pattern).WithMany().HasForeignKey(l => l.PatternId);
        builder.HasOne(l => l.Colorway).WithMany().HasForeignKey(l => l.ColorwayId);
        builder.HasOne(l => l.RecipeTrial).WithMany().HasForeignKey(l => l.RecipeTrialId);
        builder.HasOne(l => l.Machine).WithMany().HasForeignKey(l => l.MachineId);
    }
}

public sealed class BoyahaneCostSettingsConfiguration : IEntityTypeConfiguration<BoyahaneCostSettings>
{
    public void Configure(EntityTypeBuilder<BoyahaneCostSettings> builder)
    {
        builder.ToTable("CostSettings", t =>
        {
            t.HasCheckConstraint("CK_CostSettings_Method", UpperSnakeEnumConverter.CheckConstraintSql<ChemicalCostMethod>("ChemicalCostMethod"));
            t.HasCheckConstraint("CK_CostSettings_Costs", "[DefaultMachineHourlyCost] >= 0 AND [LaborCostPerMinute] >= 0");
            t.HasCheckConstraint("CK_CostSettings_Defaults", "[DefaultLiquorRatio] > 0 AND [DefaultBatchKilograms] > 0");
            t.HasCheckConstraint("CK_CostSettings_Percents", "[OverheadPercent] BETWEEN 0 AND 100 AND [FirePercent] BETWEEN 0 AND 50 AND [MarginPercent] BETWEEN -100 AND 1000");
            t.HasCheckConstraint("CK_CostSettings_Minutes", "[FabricDyeingMinutes] > 0 AND [YarnDyeingMinutes] > 0 AND [FinishingMinutes] > 0 AND [PrintingMinutes] > 0");
        });

        builder.Property(s => s.Currency).HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.Property(s => s.DefaultMachineHourlyCost).HasPrecision(12, 4);
        builder.Property(s => s.LaborCostPerMinute).HasPrecision(12, 4);
        builder.Property(s => s.DefaultLiquorRatio).HasPrecision(6, 2);
        builder.Property(s => s.DefaultBatchKilograms).HasPrecision(10, 2);
        builder.Property(s => s.OverheadPercent).HasPrecision(5, 2);
        builder.Property(s => s.FirePercent).HasPrecision(5, 2);
        builder.Property(s => s.MarginPercent).HasPrecision(7, 2);
        builder.Property(s => s.FabricDyeingMinutes).HasPrecision(10, 2);
        builder.Property(s => s.YarnDyeingMinutes).HasPrecision(10, 2);
        builder.Property(s => s.FinishingMinutes).HasPrecision(10, 2);
        builder.Property(s => s.PrintingMinutes).HasPrecision(10, 2);
    }
}

public sealed class BoyahaneMachineCostRateConfiguration : IEntityTypeConfiguration<BoyahaneMachineCostRate>
{
    public void Configure(EntityTypeBuilder<BoyahaneMachineCostRate> builder)
    {
        builder.ToTable("MachineCostRates", t => t.HasCheckConstraint("CK_MachineCostRates_HourlyCost", "[HourlyCost] >= 0"));
        builder.HasKey(r => r.MachineId);
        builder.Property(r => r.HourlyCost).HasPrecision(12, 4);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100);
        builder.HasOne(r => r.Machine).WithMany().HasForeignKey(r => r.MachineId);
    }
}
