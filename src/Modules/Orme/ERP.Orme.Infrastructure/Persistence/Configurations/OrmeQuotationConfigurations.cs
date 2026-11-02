using ERP.Core.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Orme.Infrastructure.Persistence.Configurations;

public sealed class OrmeCostSettingsConfiguration : IEntityTypeConfiguration<OrmeCostSettings>
{
    public void Configure(EntityTypeBuilder<OrmeCostSettings> builder)
    {
        builder.ToTable("CostSettings", t =>
        {
            t.HasCheckConstraint("CK_CostSettings_HourlyCosts", "[MachineHourlyCost] >= 0 AND [LaborHourlyCost] >= 0");
            t.HasCheckConstraint("CK_CostSettings_Percents",
                "[OverheadPercent] BETWEEN 0 AND 1000 AND [DefaultWastePercent] BETWEEN 0 AND 100 AND [DefaultMarginPercent] BETWEEN 0 AND 1000");
        });
        builder.Property(s => s.MachineHourlyCost).HasPrecision(18, 4);
        builder.Property(s => s.LaborHourlyCost).HasPrecision(18, 4);
        builder.Property(s => s.OverheadPercent).HasPrecision(7, 2);
        builder.Property(s => s.DefaultWastePercent).HasPrecision(5, 2);
        builder.Property(s => s.DefaultMarginPercent).HasPrecision(7, 2);
    }
}

public sealed class OrmeMachineCostRateConfiguration : IEntityTypeConfiguration<OrmeMachineCostRate>
{
    public void Configure(EntityTypeBuilder<OrmeMachineCostRate> builder)
    {
        builder.ToTable("MachineCostRates", t => t.HasCheckConstraint("CK_MachineCostRates_HourlyCost", "[HourlyCost] >= 0"));
        builder.Property(r => r.HourlyCost).HasPrecision(18, 4);
        builder.HasOne(r => r.Machine).WithMany().HasForeignKey(r => r.MachineId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(r => r.MachineId).IsUnique();
    }
}

public sealed class OrmeQuotationConfiguration : IEntityTypeConfiguration<OrmeQuotation>
{
    public void Configure(EntityTypeBuilder<OrmeQuotation> builder)
    {
        builder.ToTable("Quotations", t =>
        {
            t.HasCheckConstraint("CK_Quotations_Status", UpperSnakeEnumConverter.CheckConstraintSql<QuotationStatus>("Status"));
            t.HasCheckConstraint("CK_Quotations_DocumentLanguage", UpperSnakeEnumConverter.CheckConstraintSql<DocumentLanguage>("DocumentLanguage"));
            // Müşteri kartı ya da aday firma adı olmalıdır.
            t.HasCheckConstraint("CK_Quotations_Party", "[CustomerId] IS NOT NULL OR [ProspectName] IS NOT NULL");
            t.HasCheckConstraint("CK_Quotations_ValidUntil", "[ValidUntil] >= [QuotationDate]");
        });

        builder.Property(q => q.QuotationNo).HasMaxLength(30).IsRequired();
        builder.Property(q => q.ProspectName).HasMaxLength(200);
        builder.Property(q => q.Currency).HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.Property(q => q.Notes).HasMaxLength(1000);

        builder.HasOne(q => q.Customer).WithMany().HasForeignKey(q => q.CustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(q => q.QuotationNo).IsUnique();
        builder.HasIndex(q => q.QuotationDate);
        builder.HasIndex(q => new { q.Status, q.ValidUntil });
    }
}

public sealed class OrmeQuotationLineConfiguration : IEntityTypeConfiguration<OrmeQuotationLine>
{
    public void Configure(EntityTypeBuilder<OrmeQuotationLine> builder)
    {
        builder.ToTable("QuotationLines", t =>
        {
            t.HasCheckConstraint("CK_QuotationLines_Kilograms", "[Kilograms] > 0");
            t.HasCheckConstraint("CK_QuotationLines_UnitPrice", "[UnitPrice] >= 0");
            t.HasCheckConstraint("CK_QuotationLines_KilogramsPerHour", "[KilogramsPerHour] IS NULL OR [KilogramsPerHour] > 0");
            t.HasCheckConstraint("CK_QuotationLines_Costs", "[MachineHourlyCost] >= 0 AND [LaborHourlyCost] >= 0");
            t.HasCheckConstraint("CK_QuotationLines_Percents", "[WastePercent] BETWEEN 0 AND 100 AND [OverheadPercent] BETWEEN 0 AND 1000 AND [MarginPercent] >= -100");
        });

        builder.Property(l => l.Kilograms).HasPrecision(12, 2);
        builder.Property(l => l.WastePercent).HasPrecision(5, 2);
        builder.Property(l => l.KilogramsPerHour).HasPrecision(8, 2);
        builder.Property(l => l.MachineHourlyCost).HasPrecision(18, 4);
        builder.Property(l => l.LaborHourlyCost).HasPrecision(18, 4);
        builder.Property(l => l.OverheadPercent).HasPrecision(7, 2);
        builder.Property(l => l.MarginPercent).HasPrecision(9, 2);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.CustomerFabricCode).HasMaxLength(50);
        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.HasOne(l => l.Quotation).WithMany(q => q.Lines).HasForeignKey(l => l.QuotationId);
        builder.HasOne(l => l.Fabric).WithMany().HasForeignKey(l => l.FabricId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Machine).WithMany().HasForeignKey(l => l.MachineId).OnDelete(DeleteBehavior.Restrict);
        // Tekliften açılan sipariş satırı silinirse bağ boşalır; teklif kabul edilmiş olarak kalır.
        builder.HasOne(l => l.OrderLine).WithMany().HasForeignKey(l => l.OrderLineId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => new { l.QuotationId, l.Sequence });
        builder.HasIndex(l => l.OrderLineId).HasFilter("[OrderLineId] IS NOT NULL");
    }
}

public sealed class OrmeQuotationLineYarnConfiguration : IEntityTypeConfiguration<OrmeQuotationLineYarn>
{
    public void Configure(EntityTypeBuilder<OrmeQuotationLineYarn> builder)
    {
        builder.ToTable("QuotationLineYarns", t =>
        {
            t.HasCheckConstraint("CK_QuotationLineYarns_Percent", "[Percent] > 0 AND [Percent] <= 100");
            t.HasCheckConstraint("CK_QuotationLineYarns_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
            t.HasCheckConstraint("CK_QuotationLineYarns_PriceSource", UpperSnakeEnumConverter.CheckConstraintSql<QuotationPriceSource>("PriceSource"));
        });

        builder.Property(y => y.Percent).HasPrecision(5, 2);
        builder.Property(y => y.UnitPrice).HasPrecision(18, 4);
        builder.Property(y => y.PriceReference).HasMaxLength(100);

        builder.HasOne(y => y.Line).WithMany(l => l.Yarns).HasForeignKey(y => y.LineId);
        builder.HasOne(y => y.Yarn).WithMany().HasForeignKey(y => y.YarnId).OnDelete(DeleteBehavior.Restrict);
    }
}
