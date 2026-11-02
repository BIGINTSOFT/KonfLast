using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", t =>
        {
            t.HasCheckConstraint("CK_Customers_PartyType", UpperSnakeEnumConverter.CheckConstraintSql<PartyType>("PartyType"));
            t.HasCheckConstraint(
                "CK_Customers_EInvoiceProfile", UpperSnakeEnumConverter.CheckConstraintSql<EInvoiceProfile>("EInvoiceProfile"));
            t.HasCheckConstraint(
                "CK_Customers_EArchiveDelivery", UpperSnakeEnumConverter.CheckConstraintSql<EArchiveDelivery>("EArchiveDelivery"));
            t.HasCheckConstraint(
                "CK_Customers_DocumentLanguage", UpperSnakeEnumConverter.CheckConstraintSql<DocumentLanguage>("DocumentLanguage"));
        });

        builder.Property(c => c.CustomerCode).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.FirstName).HasMaxLength(100);
        builder.Property(c => c.LastName).HasMaxLength(100);
        builder.Property(c => c.TaxNumber).HasMaxLength(20);
        builder.Property(c => c.TaxOffice).HasMaxLength(100);
        builder.Property(c => c.MersisNo).HasMaxLength(16);
        builder.Property(c => c.TradeRegistryNo).HasMaxLength(30);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.EInvoiceAlias).HasMaxLength(256);
        builder.Property(c => c.EDespatchAlias).HasMaxLength(256);
        builder.Property(c => c.EArchiveEmail).HasMaxLength(256);
        builder.Property(c => c.Currency).HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();

        // Mevcut müşteriler Türkçe belgeyle devam eder.
        builder.Property(c => c.DocumentLanguage).HasDefaultValue(DocumentLanguage.Turkish);

        builder.HasIndex(c => c.CustomerCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
        builder.HasIndex(c => c.TaxNumber);
    }
}

public sealed class CustomerBuyerConfiguration : IEntityTypeConfiguration<CustomerBuyer>
{
    public void Configure(EntityTypeBuilder<CustomerBuyer> builder)
    {
        builder.ToTable("CustomerBuyers");

        builder.Property(b => b.Code).HasMaxLength(30).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.ContactName).HasMaxLength(100);
        builder.Property(b => b.Email).HasMaxLength(256);
        builder.Property(b => b.Phone).HasMaxLength(30);

        builder.HasOne(b => b.Customer).WithMany(c => c.Buyers).HasForeignKey(b => b.CustomerId);

        builder.HasIndex(b => new { b.CustomerId, b.Code }).IsUnique();
    }
}

public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("CustomerAddresses", t =>
            t.HasCheckConstraint("CK_CustomerAddresses_CountryCode", "LEN([CountryCode]) = 2"));

        builder.Property(a => a.Title).HasMaxLength(100).IsRequired();
        builder.Property(a => a.AddressLine).HasMaxLength(500).IsRequired();
        builder.Property(a => a.District).HasMaxLength(100).IsRequired();
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PostalCode).HasMaxLength(10);
        builder.Property(a => a.CountryCode).HasColumnType("char(2)").IsRequired();
        builder.Property(a => a.ContactName).HasMaxLength(100);
        builder.Property(a => a.Phone).HasMaxLength(30);

        builder.HasOne(a => a.Customer).WithMany(c => c.Addresses).HasForeignKey(a => a.CustomerId);

        builder.HasIndex(a => a.CustomerId);
        // Müşterinin en fazla bir varsayılan fatura ve bir varsayılan teslimat adresi olur.
        builder.HasIndex(a => a.CustomerId, "UX_CustomerAddresses_DefaultInvoice")
            .IsUnique().HasFilter("[IsDefaultInvoice] = 1");
        builder.HasIndex(a => a.CustomerId, "UX_CustomerAddresses_DefaultDelivery")
            .IsUnique().HasFilter("[IsDefaultDelivery] = 1");
    }
}

public sealed class ModelConfiguration : IEntityTypeConfiguration<Model>
{
    public void Configure(EntityTypeBuilder<Model> builder)
    {
        builder.ToTable("Models");

        builder.Property(m => m.ModelCode).HasMaxLength(30).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(500);

        builder.HasOne(m => m.Customer).WithMany().HasForeignKey(m => m.CustomerId);

        builder.HasIndex(m => m.ModelCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", t =>
        {
            t.HasCheckConstraint("CK_Products_TargetGroup", UpperSnakeEnumConverter.CheckConstraintSql<TargetGroup>("TargetGroup"));
            t.HasCheckConstraint("CK_Products_SalesUnit", UpperSnakeEnumConverter.CheckConstraintSql<StockUnit>("SalesUnit"));
            t.HasCheckConstraint("CK_Products_VatRate", "[VatRate] >= 0 AND [VatRate] <= 100");
            t.HasCheckConstraint("CK_Products_ListPrice", "[ListPrice] IS NULL OR [ListPrice] >= 0");
            t.HasCheckConstraint("CK_Products_FabricWeight", "[FabricWeight] IS NULL OR [FabricWeight] > 0");
        });

        builder.Property(p => p.ProductCode).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Season).HasMaxLength(20);
        builder.Property(p => p.CustomerArticleCode).HasMaxLength(50);
        builder.Property(p => p.Composition).HasMaxLength(200);
        builder.Property(p => p.CareInstructions).HasMaxLength(500);
        builder.Property(p => p.OriginCountryCode).HasColumnType("char(2)").IsRequired();
        builder.Property(p => p.GtipCode).HasColumnType("varchar(12)");
        builder.Property(p => p.VatRate).HasPrecision(5, 2);
        builder.Property(p => p.Currency).HasColumnType("char(3)").IsRequired();

        builder.HasOne(p => p.Model).WithMany(m => m.Products).HasForeignKey(p => p.ModelId);
        builder.HasOne(p => p.ProductGroup).WithMany().HasForeignKey(p => p.ProductGroupId);

        builder.HasIndex(p => p.ProductCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
        builder.HasIndex(p => p.ProductGroupId);
        builder.HasIndex(p => p.Season);
    }
}

public sealed class ProductGroupConfiguration : IEntityTypeConfiguration<ProductGroup>
{
    public void Configure(EntityTypeBuilder<ProductGroup> builder)
    {
        builder.ToTable("ProductGroups");
        builder.Property(g => g.Code).HasMaxLength(30).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(g => g.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class ColorConfiguration : IEntityTypeConfiguration<Color>
{
    public void Configure(EntityTypeBuilder<Color> builder)
    {
        builder.ToTable("Colors");

        builder.Property(c => c.ColorCode).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.HexCode).HasMaxLength(7).IsUnicode(false);

        builder.HasIndex(c => c.ColorCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class SizeConfiguration : IEntityTypeConfiguration<Size>
{
    public void Configure(EntityTypeBuilder<Size> builder)
    {
        builder.ToTable("Sizes");

        builder.Property(s => s.SizeCode).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(s => s.SizeCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}
