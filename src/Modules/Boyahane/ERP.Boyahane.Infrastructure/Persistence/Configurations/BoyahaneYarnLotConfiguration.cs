using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneYarnLotConfiguration : IEntityTypeConfiguration<BoyahaneYarnLot>
{
    public void Configure(EntityTypeBuilder<BoyahaneYarnLot> builder)
    {
        builder.ToTable("YarnLots", t =>
        {
            t.HasCheckConstraint("CK_YarnLots_PackageType", UpperSnakeEnumConverter.CheckConstraintSql<YarnPackageType>("PackageType"));
            t.HasCheckConstraint("CK_YarnLots_Kilograms", "[Kilograms] > 0");
            t.HasCheckConstraint("CK_YarnLots_PackageCount", "[PackageCount] >= 1");
            t.HasCheckConstraint("CK_YarnLots_Status", UpperSnakeEnumConverter.CheckConstraintSql<YarnLotStatus>("Status"));
        });

        builder.Property(l => l.LotNo).HasMaxLength(30).IsRequired();
        builder.Property(l => l.DespatchNo).HasMaxLength(50);
        builder.Property(l => l.CustomerLotNo).HasMaxLength(50);
        builder.Property(l => l.Kilograms).HasPrecision(12, 2);
        builder.Property(l => l.Notes).HasMaxLength(1000);
        builder.Property(l => l.RejectionNote).HasMaxLength(500);

        builder.HasOne(l => l.Customer).WithMany().HasForeignKey(l => l.CustomerId);
        builder.HasOne(l => l.Yarn).WithMany().HasForeignKey(l => l.YarnId);
        builder.HasOne(l => l.OrderLine).WithMany(o => o.YarnLots).HasForeignKey(l => l.OrderLineId);

        builder.HasIndex(l => l.LotNo).IsUnique();
    }
}
