using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class CostSettingConfiguration : IEntityTypeConfiguration<CostSetting>
{
    public void Configure(EntityTypeBuilder<CostSetting> builder)
    {
        builder.ToTable("CostSettings", t =>
        {
            t.HasCheckConstraint("CK_CostSettings_Rates", "[LaborCostPerMinute] >= 0 AND [EnergyCostPerMinute] >= 0");
            t.HasCheckConstraint(
                "CK_CostSettings_Percents",
                "[OverheadPercent] >= 0 AND [DefaultFirePercent] >= 0 AND [DefaultMarginPercent] >= 0");
        });

        // Ayarlar tek satırdır; kimliği sabittir.
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.LaborCostPerMinute).HasPrecision(18, 4);
        builder.Property(s => s.EnergyCostPerMinute).HasPrecision(18, 4);
        builder.Property(s => s.Currency).HasColumnType("char(3)").IsRequired();
        builder.Property(s => s.OverheadPercent).HasPrecision(7, 2);
        builder.Property(s => s.DefaultFirePercent).HasPrecision(7, 2);
        builder.Property(s => s.DefaultMarginPercent).HasPrecision(7, 2);

        builder.HasData(new CostSetting
        {
            Id = CostSetting.SingletonId,
            Currency = "TRY",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "system",
        });
    }
}

public sealed class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable("Quotations", t =>
        {
            t.HasCheckConstraint("CK_Quotations_Status", UpperSnakeEnumConverter.CheckConstraintSql<QuotationStatus>("Status"));
            t.HasCheckConstraint(
                "CK_Quotations_DocumentLanguage", UpperSnakeEnumConverter.CheckConstraintSql<DocumentLanguage>("DocumentLanguage"));
            t.HasCheckConstraint("CK_Quotations_Validity", "[ValidUntil] >= [QuotationDate]");
            t.HasCheckConstraint("CK_Quotations_Party", "[CustomerId] IS NOT NULL OR [ProspectName] IS NOT NULL");
        });

        builder.Property(q => q.QuotationNo).HasMaxLength(30).IsRequired();
        builder.Property(q => q.ProspectName).HasMaxLength(200);
        builder.Property(q => q.Currency).HasColumnType("char(3)").IsRequired();
        builder.Property(q => q.Notes).HasMaxLength(2000);
        builder.Property(q => q.LaborCostPerMinute).HasPrecision(18, 4);
        builder.Property(q => q.EnergyCostPerMinute).HasPrecision(18, 4);
        builder.Property(q => q.DecidedBy).HasMaxLength(100);
        builder.Property(q => q.RowVersion).IsRowVersion();

        builder.Ignore(q => q.IsEditable);
        builder.Ignore(q => q.AllowedTransitions);

        builder.HasOne(q => q.Customer).WithMany().HasForeignKey(q => q.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(q => q.SalesOrder).WithMany().HasForeignKey(q => q.SalesOrderId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(q => q.QuotationNo).IsUnique();
        builder.HasIndex(q => new { q.CustomerId, q.QuotationDate });
        builder.HasIndex(q => q.Status);
        builder.HasIndex(q => q.SalesOrderId);
    }
}

public sealed class QuotationLineConfiguration : IEntityTypeConfiguration<QuotationLine>
{
    public void Configure(EntityTypeBuilder<QuotationLine> builder)
    {
        builder.ToTable("QuotationLines", t =>
        {
            t.HasCheckConstraint("CK_QuotationLines_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_QuotationLines_Item", "[ProductId] IS NOT NULL OR [Description] IS NOT NULL");
        });

        builder.Property(l => l.Description).HasMaxLength(200);
        builder.Property(l => l.ModelReference).HasMaxLength(100);
        builder.Property(l => l.Colors).HasMaxLength(200);
        builder.Property(l => l.Warnings).HasMaxLength(2000);
        builder.Property(l => l.ManualMinutes).HasPrecision(9, 4);
        builder.Property(l => l.ManualSubcontractCost).HasPrecision(18, 4);
        builder.Property(l => l.RouteMinutes).HasPrecision(9, 4);
        builder.Property(l => l.RouteSubcontractCost).HasPrecision(18, 4);
        builder.Property(l => l.FirePercent).HasPrecision(7, 2);
        builder.Property(l => l.OverheadPercent).HasPrecision(7, 2);
        builder.Property(l => l.MarginPercent).HasPrecision(9, 2);
        builder.Property(l => l.MaterialCost).HasPrecision(18, 4);
        builder.Property(l => l.LaborMinutes).HasPrecision(9, 4);
        builder.Property(l => l.LaborCost).HasPrecision(18, 4);
        builder.Property(l => l.EnergyCost).HasPrecision(18, 4);
        builder.Property(l => l.SubcontractCost).HasPrecision(18, 4);
        builder.Property(l => l.FireCost).HasPrecision(18, 4);
        builder.Property(l => l.OverheadCost).HasPrecision(18, 4);
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);

        builder.Ignore(l => l.Amount);

        builder.HasOne(l => l.Quotation).WithMany(q => q.Lines).HasForeignKey(l => l.QuotationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.QuotationId, l.LineNo }).IsUnique();
        builder.HasIndex(l => l.ProductId);
    }
}

public sealed class QuotationLineMaterialConfiguration : IEntityTypeConfiguration<QuotationLineMaterial>
{
    public void Configure(EntityTypeBuilder<QuotationLineMaterial> builder)
    {
        builder.ToTable("QuotationLineMaterials", t =>
            t.HasCheckConstraint("CK_QuotationLineMaterials_Values", "[QuantityPerPiece] >= 0 AND [UnitCost] >= 0"));

        builder.Property(m => m.Description).HasMaxLength(200).IsRequired();
        builder.Property(m => m.QuantityPerPiece).HasPrecision(18, 4);
        builder.Property(m => m.UnitCost).HasPrecision(18, 4);

        builder.Ignore(m => m.Cost);

        builder.HasOne(m => m.QuotationLine).WithMany(l => l.Materials).HasForeignKey(m => m.QuotationLineId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.StockItem).WithMany().HasForeignKey(m => m.StockItemId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.QuotationLineId);
        builder.HasIndex(m => m.StockItemId);
    }
}
