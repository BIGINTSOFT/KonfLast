using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneOrderConfiguration : IEntityTypeConfiguration<BoyahaneOrder>
{
    public void Configure(EntityTypeBuilder<BoyahaneOrder> builder)
    {
        builder.ToTable("Orders", t =>
        {
            t.HasCheckConstraint("CK_Orders_Ownership", UpperSnakeEnumConverter.CheckConstraintSql<OrderOwnership>("Ownership"));
            t.HasCheckConstraint("CK_Orders_Status", UpperSnakeEnumConverter.CheckConstraintSql<OrderStatus>("Status"));
        });

        builder.Property(o => o.OrderNo).HasMaxLength(30).IsRequired();
        builder.Property(o => o.CustomerOrderNo).HasMaxLength(50);
        builder.Property(o => o.Currency).HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.ClosedBy).HasMaxLength(100);
        builder.Property(o => o.CloseNote).HasMaxLength(500);

        builder.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId);

        builder.HasIndex(o => o.OrderNo).IsUnique();
        builder.HasIndex(o => o.OrderDate);
        builder.HasIndex(o => o.Status);
    }
}

public sealed class BoyahaneOrderLineConfiguration : IEntityTypeConfiguration<BoyahaneOrderLine>
{
    public void Configure(EntityTypeBuilder<BoyahaneOrderLine> builder)
    {
        builder.ToTable("OrderLines", t =>
        {
            t.HasCheckConstraint("CK_OrderLines_ProcessType", UpperSnakeEnumConverter.CheckConstraintSql<OrderProcessType>("ProcessType"));
            t.HasCheckConstraint("CK_OrderLines_Unit", "[Unit] IN ('METER', 'KILOGRAM')");
            t.HasCheckConstraint("CK_OrderLines_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_OrderLines_Item", "([FabricId] IS NOT NULL AND [YarnId] IS NULL) OR ([FabricId] IS NULL AND [YarnId] IS NOT NULL)");
        });

        builder.Property(l => l.ColorName).HasMaxLength(100);
        builder.Property(l => l.ColorCode).HasMaxLength(50);
        builder.Property(l => l.Quantity).HasPrecision(12, 2);
        builder.Property(l => l.UnitPrice).HasPrecision(12, 4);
        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.HasOne(l => l.Order).WithMany(o => o.Lines).HasForeignKey(l => l.OrderId);
        builder.HasOne(l => l.Fabric).WithMany().HasForeignKey(l => l.FabricId);
        builder.HasOne(l => l.Yarn).WithMany().HasForeignKey(l => l.YarnId);
        builder.HasOne(l => l.ColorCard).WithMany().HasForeignKey(l => l.ColorCardId);
        builder.HasOne(l => l.Pattern).WithMany().HasForeignKey(l => l.PatternId);
        builder.HasOne(l => l.Colorway).WithMany().HasForeignKey(l => l.ColorwayId);

        builder.HasIndex(l => l.ColorCardId);
        builder.HasIndex(l => l.PatternId);
        builder.HasIndex(l => l.ColorwayId);
    }
}
