using ERP.Orme.Domain.Entities;
using ERP.Core.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Orme.Infrastructure.Persistence.Configurations;

public sealed class OrmeCustomerConfiguration : IEntityTypeConfiguration<OrmeCustomer>
{
    public void Configure(EntityTypeBuilder<OrmeCustomer> builder)
    {
        builder.ToTable("Customers", t =>
        {
            t.HasCheckConstraint("CK_Customers_PartyType", UpperSnakeEnumConverter.CheckConstraintSql<PartyType>("PartyType"));
            t.HasCheckConstraint(
                "CK_Customers_EInvoiceProfile", UpperSnakeEnumConverter.CheckConstraintSql<EInvoiceProfile>("EInvoiceProfile"));
            t.HasCheckConstraint(
                "CK_Customers_EArchiveDelivery", UpperSnakeEnumConverter.CheckConstraintSql<EArchiveDelivery>("EArchiveDelivery"));
            t.HasCheckConstraint(
                "CK_Customers_FireTolerancePercent", "[FireTolerancePercent] IS NULL OR [FireTolerancePercent] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_Customers_PaymentTermDays", "[PaymentTermDays] IS NULL OR [PaymentTermDays] >= 0");
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
        builder.Property(c => c.VatRate).HasPrecision(5, 2);
        builder.Property(c => c.FireTolerancePercent).HasPrecision(5, 2);
        builder.Property(c => c.FirstQualityMaxPoints).HasPrecision(8, 2);
        builder.Property(c => c.SecondQualityMaxPoints).HasPrecision(8, 2);
        builder.Property(c => c.SecondQualityDiscountPercent).HasPrecision(5, 2);
        // Var olan müşteriler Türkçe belgeyle devam eder.
        builder.Property(c => c.DocumentLanguage).HasDefaultValue(DocumentLanguage.Turkish);

        builder.HasIndex(c => c.CustomerCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
        builder.HasIndex(c => c.TaxNumber);
    }
}

public sealed class OrmeCustomerAddressConfiguration : IEntityTypeConfiguration<OrmeCustomerAddress>
{
    public void Configure(EntityTypeBuilder<OrmeCustomerAddress> builder)
    {
        builder.ToTable("CustomerAddresses", t =>
            t.HasCheckConstraint("CK_CustomerAddresses_CountryCode", "LEN([CountryCode]) = 2"));

        builder.Property(a => a.Title).HasMaxLength(100).IsRequired();
        builder.Property(a => a.AddressLine).HasMaxLength(500).IsRequired();
        builder.Property(a => a.District).HasMaxLength(100).IsRequired();
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PostalCode).HasMaxLength(10);
        builder.Property(a => a.CountryCode).HasColumnType("char(2)").HasDefaultValue("TR").IsRequired();

        builder.HasOne(a => a.Customer).WithMany(c => c.Addresses).HasForeignKey(a => a.CustomerId);

        // Müşterinin en fazla bir varsayılan fatura ve bir varsayılan teslimat adresi olur.
        builder.HasIndex(a => a.CustomerId, "UX_CustomerAddresses_DefaultInvoice")
            .IsUnique().HasFilter("[IsDefaultInvoice] = 1");
        builder.HasIndex(a => a.CustomerId, "UX_CustomerAddresses_DefaultDelivery")
            .IsUnique().HasFilter("[IsDefaultDelivery] = 1");
    }
}

public sealed class OrmeCustomerContactConfiguration : IEntityTypeConfiguration<OrmeCustomerContact>
{
    public void Configure(EntityTypeBuilder<OrmeCustomerContact> builder)
    {
        builder.ToTable("CustomerContacts");

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Title).HasMaxLength(100);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(256);

        builder.HasOne(c => c.Customer).WithMany(p => p.Contacts).HasForeignKey(c => c.CustomerId);
    }
}
