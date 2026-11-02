using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Konfeksiyon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class AssortmentTemplateConfiguration : IEntityTypeConfiguration<AssortmentTemplate>
{
    public void Configure(EntityTypeBuilder<AssortmentTemplate> builder)
    {
        builder.ToTable("AssortmentTemplates");

        builder.Property(t => t.Code).HasMaxLength(30).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.Ignore(t => t.PiecesPerCarton);

        builder.HasOne(t => t.Customer).WithMany().HasForeignKey(t => t.CustomerId);

        builder.HasIndex(t => t.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
        builder.HasIndex(t => t.CustomerId);
    }
}

public sealed class AssortmentTemplateLineConfiguration : IEntityTypeConfiguration<AssortmentTemplateLine>
{
    public void Configure(EntityTypeBuilder<AssortmentTemplateLine> builder)
    {
        builder.ToTable("AssortmentTemplateLines", t => t.HasCheckConstraint("CK_AssortmentTemplateLines_Ratio", "[Ratio] > 0"));

        builder.HasOne(l => l.AssortmentTemplate).WithMany(t => t.Lines).HasForeignKey(l => l.AssortmentTemplateId);
        builder.HasOne(l => l.Size).WithMany().HasForeignKey(l => l.SizeId);

        // Şablonda bir beden bir kez yer alır.
        builder.HasIndex(l => new { l.AssortmentTemplateId, l.SizeId }).IsUnique();
        builder.HasIndex(l => l.SizeId);

        builder.HasQueryFilter(l => !l.AssortmentTemplate.IsDeleted);
    }
}

public sealed class ShipmentCartonConfiguration : IEntityTypeConfiguration<ShipmentCarton>
{
    public void Configure(EntityTypeBuilder<ShipmentCarton> builder)
    {
        builder.ToTable("ShipmentCartons", t =>
        {
            t.HasCheckConstraint("CK_ShipmentCartons_CartonNo", "[CartonNo] > 0");
            t.HasCheckConstraint("CK_ShipmentCartons_Weight", "([NetKg] IS NULL OR [NetKg] >= 0) AND ([GrossKg] IS NULL OR [GrossKg] >= 0)");

            // Yüklenmiş kolinin ne zaman ve kim tarafından okutulduğu bellidir.
            t.HasCheckConstraint("CK_ShipmentCartons_Loaded", "([LoadedAt] IS NULL AND [LoadedBy] IS NULL) OR ([LoadedAt] IS NOT NULL AND [LoadedBy] IS NOT NULL)");
        });

        builder.Property(c => c.NetKg).HasPrecision(10, 3);
        builder.Property(c => c.GrossKg).HasPrecision(10, 3);
        builder.Property(c => c.LoadedBy).HasMaxLength(100);
        builder.Ignore(c => c.IsLoaded);

        builder.HasOne(c => c.Shipment).WithMany(s => s.Cartons).HasForeignKey(c => c.ShipmentId);
        builder.HasOne(c => c.AssortmentTemplate).WithMany().HasForeignKey(c => c.AssortmentTemplateId);

        // Koli numarası sevkiyat içinde tekildir (koli kodu numaradan üretilir).
        builder.HasIndex(c => new { c.ShipmentId, c.CartonNo }).IsUnique();
        builder.HasIndex(c => c.AssortmentTemplateId);

        builder.HasQueryFilter(c => !c.Shipment.IsDeleted);
    }
}

public sealed class ShipmentCartonLineConfiguration : IEntityTypeConfiguration<ShipmentCartonLine>
{
    public void Configure(EntityTypeBuilder<ShipmentCartonLine> builder)
    {
        builder.ToTable("ShipmentCartonLines", t => t.HasCheckConstraint("CK_ShipmentCartonLines_Quantity", "[Quantity] > 0"));

        builder.Property(l => l.Quantity).HasPrecision(18, 3);

        builder.HasOne(l => l.Carton).WithMany(c => c.Lines).HasForeignKey(l => l.ShipmentCartonId);
        builder.HasOne(l => l.StockItem).WithMany().HasForeignKey(l => l.StockItemId);

        // Bir mamul kartı kolide tek satırdır.
        builder.HasIndex(l => new { l.ShipmentCartonId, l.StockItemId }).IsUnique();
        builder.HasIndex(l => l.StockItemId);

        builder.HasQueryFilter(l => !l.Carton.Shipment.IsDeleted);
    }
}
