using ERP.Core.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Dokuma.Infrastructure.Persistence.Configurations;

public sealed class DokumaCostSettingsConfiguration : IEntityTypeConfiguration<DokumaCostSettings>
{
    public void Configure(EntityTypeBuilder<DokumaCostSettings> builder)
    {
        builder.ToTable("CostSettings", t =>
        {
            t.HasCheckConstraint(
                "CK_CostSettings_Amounts",
                "[LoomHourlyCost] >= 0 AND [LaborHourlyCost] >= 0 AND [WarpingCostPerMeter] >= 0 AND [SizingCostPerMeter] >= 0");
            t.HasCheckConstraint(
                "CK_CostSettings_Percents",
                "[OverheadPercent] BETWEEN 0 AND 500 AND [DefaultWastePercent] BETWEEN 0 AND 100 AND [DefaultMarginPercent] BETWEEN 0 AND 500");
            t.HasCheckConstraint("CK_CostSettings_Efficiency", "[DefaultEfficiencyPercent] > 0 AND [DefaultEfficiencyPercent] <= 100");
            t.HasCheckConstraint("CK_CostSettings_ValidityDays", "[QuotationValidityDays] BETWEEN 1 AND 365");
        });

        builder.Property(s => s.Currency).HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.Property(s => s.LoomHourlyCost).HasPrecision(18, 4);
        builder.Property(s => s.LaborHourlyCost).HasPrecision(18, 4);
        builder.Property(s => s.WarpingCostPerMeter).HasPrecision(18, 4);
        builder.Property(s => s.SizingCostPerMeter).HasPrecision(18, 4);
        builder.Property(s => s.OverheadPercent).HasPrecision(6, 2);
        builder.Property(s => s.DefaultWastePercent).HasPrecision(5, 2);
        builder.Property(s => s.DefaultMarginPercent).HasPrecision(6, 2);
        builder.Property(s => s.DefaultEfficiencyPercent).HasPrecision(5, 2).HasDefaultValue(85m);
        builder.Property(s => s.QuotationValidityDays).HasDefaultValue(15);
    }
}

public sealed class DokumaLoomCostRateConfiguration : IEntityTypeConfiguration<DokumaLoomCostRate>
{
    public void Configure(EntityTypeBuilder<DokumaLoomCostRate> builder)
    {
        builder.ToTable("LoomCostRates", t =>
        {
            t.HasCheckConstraint("CK_LoomCostRates_HourlyCost", "[HourlyCost] IS NULL OR [HourlyCost] >= 0");
            t.HasCheckConstraint("CK_LoomCostRates_Efficiency", "[EfficiencyPercent] IS NULL OR ([EfficiencyPercent] > 0 AND [EfficiencyPercent] <= 100)");
        });

        builder.Property(r => r.HourlyCost).HasPrecision(18, 4);
        builder.Property(r => r.EfficiencyPercent).HasPrecision(5, 2);

        builder.HasOne(r => r.Loom).WithMany().HasForeignKey(r => r.LoomId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(r => r.LoomId).IsUnique();
    }
}

public sealed class DokumaQuotationConfiguration : IEntityTypeConfiguration<DokumaQuotation>
{
    public void Configure(EntityTypeBuilder<DokumaQuotation> builder)
    {
        builder.ToTable("Quotations", t =>
        {
            t.HasCheckConstraint("CK_Quotations_Status", UpperSnakeEnumConverter.CheckConstraintSql<QuotationStatus>("Status"));
            t.HasCheckConstraint("CK_Quotations_Ownership", UpperSnakeEnumConverter.CheckConstraintSql<OrderOwnership>("Ownership"));
            t.HasCheckConstraint("CK_Quotations_DocumentLanguage", UpperSnakeEnumConverter.CheckConstraintSql<DocumentLanguage>("DocumentLanguage"));
            t.HasCheckConstraint("CK_Quotations_ValidUntil", "[ValidUntil] >= [QuotationDate]");
            // Ya kayıtlı müşteri ya da aday firma adı olmalıdır.
            t.HasCheckConstraint("CK_Quotations_Party", "[CustomerId] IS NOT NULL OR [ProspectName] IS NOT NULL");
            t.HasCheckConstraint("CK_Quotations_PaymentTermDays", "[PaymentTermDays] IS NULL OR [PaymentTermDays] BETWEEN 0 AND 365");
        });

        builder.Property(q => q.QuotationNo).HasMaxLength(30).IsRequired();
        builder.Property(q => q.ProspectName).HasMaxLength(200);
        builder.Property(q => q.ContactName).HasMaxLength(100);
        builder.Property(q => q.Currency).HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.Property(q => q.DeliveryTerms).HasMaxLength(200);
        builder.Property(q => q.Notes).HasMaxLength(1000);
        builder.Property(q => q.DecidedBy).HasMaxLength(100);
        builder.Property(q => q.RejectionReason).HasMaxLength(500);

        builder.HasOne(q => q.Customer).WithMany().HasForeignKey(q => q.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(q => q.Order).WithMany().HasForeignKey(q => q.OrderId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(q => q.QuotationNo).IsUnique();
        builder.HasIndex(q => q.QuotationDate);
        builder.HasIndex(q => q.Status);
    }
}

public sealed class DokumaQuotationLineConfiguration : IEntityTypeConfiguration<DokumaQuotationLine>
{
    public void Configure(EntityTypeBuilder<DokumaQuotationLine> builder)
    {
        builder.ToTable("QuotationLines", t =>
        {
            t.HasCheckConstraint("CK_QuotationLines_Meters", "[Meters] > 0");
            t.HasCheckConstraint("CK_QuotationLines_UnitPrice", "[UnitPrice] >= 0");
            t.HasCheckConstraint("CK_QuotationLines_Rates", "[SpeedRpm] > 0 AND [EfficiencyPercent] > 0 AND [EfficiencyPercent] <= 100 AND [PicksPerCm] > 0");
        });

        builder.Property(l => l.Meters).HasPrecision(12, 2);
        builder.Property(l => l.CustomerArticleCode).HasMaxLength(50);
        builder.Property(l => l.Notes).HasMaxLength(500);
        builder.Property(l => l.SpeedRpm).HasPrecision(8, 2);
        builder.Property(l => l.EfficiencyPercent).HasPrecision(5, 2);
        builder.Property(l => l.PicksPerCm).HasPrecision(8, 2);
        builder.Property(l => l.WarpKilogramsPerMeter).HasPrecision(12, 6);
        builder.Property(l => l.WeftKilogramsPerMeter).HasPrecision(12, 6);
        builder.Property(l => l.WeavingHoursPerMeter).HasPrecision(12, 6);
        builder.Property(l => l.WastePercent).HasPrecision(5, 2);
        builder.Property(l => l.OverheadPercent).HasPrecision(6, 2);
        builder.Property(l => l.MarginPercent).HasPrecision(6, 2);
        foreach (var amount in new[]
                 {
                     nameof(DokumaQuotationLine.YarnCost), nameof(DokumaQuotationLine.SizingCostPerWarpMeter), nameof(DokumaQuotationLine.WarpingCostPerWarpMeter),
                     nameof(DokumaQuotationLine.SizingCost), nameof(DokumaQuotationLine.WarpingCost), nameof(DokumaQuotationLine.LoomHourlyCost),
                     nameof(DokumaQuotationLine.LaborHourlyCost), nameof(DokumaQuotationLine.WeavingCost), nameof(DokumaQuotationLine.CostPerMeter),
                     nameof(DokumaQuotationLine.CalculatedPrice), nameof(DokumaQuotationLine.UnitPrice),
                 })
        {
            builder.Property<decimal>(amount).HasPrecision(18, 4);
        }

        builder.HasOne(l => l.Quotation).WithMany(q => q.Lines).HasForeignKey(l => l.QuotationId);
        builder.HasOne(l => l.Article).WithMany().HasForeignKey(l => l.ArticleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Loom).WithMany().HasForeignKey(l => l.LoomId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.QuotationId, l.Sequence }).IsUnique();
    }
}

public sealed class DokumaQuotationLineYarnConfiguration : IEntityTypeConfiguration<DokumaQuotationLineYarn>
{
    public void Configure(EntityTypeBuilder<DokumaQuotationLineYarn> builder)
    {
        builder.ToTable("QuotationLineYarns", t =>
        {
            t.HasCheckConstraint("CK_QuotationLineYarns_Kilograms", "[KilogramsPerMeter] >= 0");
            t.HasCheckConstraint("CK_QuotationLineYarns_UnitPrice", "[UnitPrice] >= 0");
        });

        builder.Property(y => y.KilogramsPerMeter).HasPrecision(12, 6);
        builder.Property(y => y.UnitPrice).HasPrecision(18, 4);
        builder.Property(y => y.PriceSource).HasMaxLength(100);

        builder.HasOne(y => y.Line).WithMany(l => l.Yarns).HasForeignKey(y => y.LineId);
        builder.HasOne(y => y.Yarn).WithMany().HasForeignKey(y => y.YarnId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(y => new { y.LineId, y.YarnId }).IsUnique();
    }
}
