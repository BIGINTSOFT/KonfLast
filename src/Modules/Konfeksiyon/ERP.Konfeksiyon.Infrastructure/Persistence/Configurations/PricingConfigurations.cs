using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Konfeksiyon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.ToTable("PriceLists", t =>
            t.HasCheckConstraint("CK_PriceLists_Validity", "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]"));

        builder.Property(p => p.Code).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Currency).HasColumnType("char(3)").IsRequired();
        builder.Property(p => p.Notes).HasMaxLength(500);

        builder.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId);

        builder.HasIndex(p => p.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
        builder.HasIndex(p => new { p.CustomerId, p.ValidFrom });
    }
}

public sealed class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
    public void Configure(EntityTypeBuilder<PriceListItem> builder)
    {
        builder.ToTable("PriceListItems", t => t.HasCheckConstraint("CK_PriceListItems_UnitPrice", "[UnitPrice] >= 0"));

        builder.Ignore(i => i.Specificity);

        builder.HasOne(i => i.PriceList).WithMany(p => p.Items).HasForeignKey(i => i.PriceListId);
        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId);
        builder.HasOne(i => i.Color).WithMany().HasForeignKey(i => i.ColorId);
        builder.HasOne(i => i.Size).WithMany().HasForeignKey(i => i.SizeId);

        // Listede bir ürün / renk / beden birleşimi bir kez yer alır (boş renk / beden de bir değerdir).
        builder.HasIndex(i => new { i.PriceListId, i.ProductId, i.ColorId, i.SizeId }).IsUnique().HasFilter(null);
        builder.HasIndex(i => i.ProductId);
    }
}

public sealed class CustomerDiscountConfiguration : IEntityTypeConfiguration<CustomerDiscount>
{
    public void Configure(EntityTypeBuilder<CustomerDiscount> builder)
    {
        builder.ToTable("CustomerDiscounts", t =>
        {
            t.HasCheckConstraint("CK_CustomerDiscounts_Percent", "[DiscountPercent] > 0 AND [DiscountPercent] < 100");
            t.HasCheckConstraint("CK_CustomerDiscounts_Validity", "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]");
        });

        builder.Property(d => d.DiscountPercent).HasPrecision(5, 2);
        builder.Property(d => d.Notes).HasMaxLength(500);

        builder.HasOne(d => d.Customer).WithMany().HasForeignKey(d => d.CustomerId);
        builder.HasOne(d => d.Product).WithMany().HasForeignKey(d => d.ProductId);

        builder.HasIndex(d => new { d.CustomerId, d.ProductId, d.ValidFrom });
    }
}
