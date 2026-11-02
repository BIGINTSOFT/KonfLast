using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneGoodsReturnConfiguration : IEntityTypeConfiguration<BoyahaneGoodsReturn>
{
    public void Configure(EntityTypeBuilder<BoyahaneGoodsReturn> builder)
    {
        builder.ToTable("GoodsReturns", t =>
        {
            t.HasCheckConstraint("CK_GoodsReturns_Kind", UpperSnakeEnumConverter.CheckConstraintSql<GoodsReturnKind>("Kind"));
            t.HasCheckConstraint("CK_GoodsReturns_Status", UpperSnakeEnumConverter.CheckConstraintSql<GoodsReturnStatus>("Status"));
        });
        builder.Property(r => r.ReturnNo).HasMaxLength(30).IsRequired();
        builder.Property(r => r.VehiclePlate).HasMaxLength(20);
        builder.Property(r => r.DriverName).HasMaxLength(100);
        builder.Property(r => r.DeliveryAddress).HasMaxLength(500);
        builder.Property(r => r.Notes).HasMaxLength(1000);
        builder.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.ReturnNo).IsUnique();
    }
}

public sealed class BoyahaneGoodsReturnItemConfiguration : IEntityTypeConfiguration<BoyahaneGoodsReturnItem>
{
    public void Configure(EntityTypeBuilder<BoyahaneGoodsReturnItem> builder)
    {
        builder.ToTable("GoodsReturnItems", t => t.HasCheckConstraint("CK_GoodsReturnItems_Item", "([RollId] IS NOT NULL AND [YarnLotId] IS NULL) OR ([RollId] IS NULL AND [YarnLotId] IS NOT NULL)"));
        builder.Property(i => i.ItemNo).HasMaxLength(40).IsRequired();
        builder.Property(i => i.CardName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Kilograms).HasPrecision(12, 2);
        builder.Property(i => i.Meters).HasPrecision(12, 2);
        builder.Property(i => i.Reason).HasMaxLength(1000);
        builder.HasOne(i => i.Return).WithMany(r => r.Items).HasForeignKey(i => i.ReturnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Roll).WithMany().HasForeignKey(i => i.RollId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.YarnLot).WithMany().HasForeignKey(i => i.YarnLotId).OnDelete(DeleteBehavior.Restrict);
    }
}
