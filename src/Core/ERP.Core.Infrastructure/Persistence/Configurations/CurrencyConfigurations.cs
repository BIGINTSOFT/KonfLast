using ERP.Core.Domain.Entities;
using ERP.Core.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Core.Infrastructure.Persistence.Configurations;

public sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies", t =>
            t.HasCheckConstraint("CK_Currencies_DecimalPlaces", "[DecimalPlaces] BETWEEN 0 AND 4"));

        builder.Property(c => c.Code).HasColumnType("char(3)").IsRequired();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Symbol).HasMaxLength(5);
        builder.Ignore(c => c.IsLocal);

        builder.HasIndex(c => c.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("ExchangeRates", t =>
        {
            t.HasCheckConstraint("CK_ExchangeRates_Source", UpperSnakeEnumConverter.CheckConstraintSql<ExchangeRateSource>("Source"));
            t.HasCheckConstraint(
                "CK_ExchangeRates_Positive",
                "[ForexBuying] > 0 AND [ForexSelling] > 0 AND ([BanknoteBuying] IS NULL OR [BanknoteBuying] > 0) "
                + "AND ([BanknoteSelling] IS NULL OR [BanknoteSelling] > 0)");
        });

        builder.Property(r => r.ForexBuying).HasPrecision(18, 6);
        builder.Property(r => r.ForexSelling).HasPrecision(18, 6);
        builder.Property(r => r.BanknoteBuying).HasPrecision(18, 6);
        builder.Property(r => r.BanknoteSelling).HasPrecision(18, 6);

        builder.HasOne(r => r.Currency).WithMany().HasForeignKey(r => r.CurrencyId);

        // Bir döviz için günde tek kur; son kuru bulmak için tarih sırası.
        builder.HasIndex(r => new { r.CurrencyId, r.RateDate }).IsUnique();
        builder.HasIndex(r => r.RateDate);
    }
}
