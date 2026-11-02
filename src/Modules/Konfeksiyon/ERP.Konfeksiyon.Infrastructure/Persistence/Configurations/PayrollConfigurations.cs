using ERP.Konfeksiyon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class PayrollSettingConfiguration : IEntityTypeConfiguration<PayrollSetting>
{
    public void Configure(EntityTypeBuilder<PayrollSetting> builder)
    {
        builder.ToTable("PayrollSettings", t => t.HasCheckConstraint("CK_PayrollSettings_MinuteRate", "[MinuteRate] >= 0"));

        // Ayarlar tek satırdır; kimliği sabittir.
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.MinuteRate).HasPrecision(18, 4);
        builder.Property(s => s.Currency).HasMaxLength(3).IsUnicode(false).IsRequired();

        builder.HasData(new PayrollSetting
        {
            Id = PayrollSetting.SingletonId,
            MinuteRate = 0,
            Currency = "TRY",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "system",
        });
    }
}

public sealed class PayrollBonusTierConfiguration : IEntityTypeConfiguration<PayrollBonusTier>
{
    public void Configure(EntityTypeBuilder<PayrollBonusTier> builder)
    {
        builder.ToTable("PayrollBonusTiers", t => t.HasCheckConstraint(
            "CK_PayrollBonusTiers_Percents", "[MinEfficiencyPercent] > 0 AND [BonusPercent] > 0"));

        builder.Property(t => t.MinEfficiencyPercent).HasPrecision(7, 2);
        builder.Property(t => t.BonusPercent).HasPrecision(7, 2);

        builder.HasOne(t => t.PayrollSetting).WithMany(s => s.BonusTiers).HasForeignKey(t => t.PayrollSettingId);

        builder.HasIndex(t => new { t.PayrollSettingId, t.MinEfficiencyPercent }).IsUnique();
    }
}
