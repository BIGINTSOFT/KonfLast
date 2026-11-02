using ERP.Core.Infrastructure.Persistence.Converters;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Dokuma.Infrastructure.Persistence.Configurations;

public sealed class DokumaOrderConfiguration : IEntityTypeConfiguration<DokumaOrder>
{
    public void Configure(EntityTypeBuilder<DokumaOrder> builder)
    {
        builder.ToTable("Orders", t =>
        {
            t.HasCheckConstraint("CK_Orders_Ownership", UpperSnakeEnumConverter.CheckConstraintSql<OrderOwnership>("Ownership"));
            t.HasCheckConstraint("CK_Orders_Status", UpperSnakeEnumConverter.CheckConstraintSql<OrderStatus>("Status"));
            // Fason siparişin müşterisi olmalıdır; kendi malında boşsa stoğa üretimdir.
            t.HasCheckConstraint("CK_Orders_CommissionCustomer", "[Ownership] <> 'COMMISSION' OR [CustomerId] IS NOT NULL");
        });

        builder.Property(o => o.OrderNo).HasMaxLength(30).IsRequired();
        builder.Property(o => o.CustomerOrderNo).HasMaxLength(50);
        builder.Property(o => o.Currency).HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(1000);

        builder.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.OrderNo).IsUnique();
        builder.HasIndex(o => o.OrderDate);
    }
}

public sealed class DokumaOrderLineConfiguration : IEntityTypeConfiguration<DokumaOrderLine>
{
    public void Configure(EntityTypeBuilder<DokumaOrderLine> builder)
    {
        builder.ToTable("OrderLines", t =>
        {
            t.HasCheckConstraint("CK_OrderLines_Meters", "[Meters] > 0");
            t.HasCheckConstraint("CK_OrderLines_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
        });

        builder.Property(l => l.Meters).HasPrecision(12, 2);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.CustomerArticleCode).HasMaxLength(50);
        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.HasOne(l => l.Order).WithMany(o => o.Lines).HasForeignKey(l => l.OrderId);
        builder.HasOne(l => l.Article).WithMany().HasForeignKey(l => l.ArticleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DokumaYarnLotConfiguration : IEntityTypeConfiguration<DokumaYarnLot>
{
    public void Configure(EntityTypeBuilder<DokumaYarnLot> builder)
    {
        builder.ToTable("YarnLots", t =>
        {
            t.HasCheckConstraint("CK_YarnLots_Kilograms", "[Kilograms] > 0");
            t.HasCheckConstraint("CK_YarnLots_PackageCount", "[PackageCount] >= 1");
        });

        builder.Property(l => l.LotNo).HasMaxLength(30).IsRequired();
        builder.Property(l => l.DespatchNo).HasMaxLength(50);
        builder.Property(l => l.ProducerLotNo).HasMaxLength(50);
        builder.Property(l => l.Kilograms).HasPrecision(12, 2);
        builder.Property(l => l.Notes).HasMaxLength(1000);

        builder.HasOne(l => l.Customer).WithMany().HasForeignKey(l => l.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Supplier).WithMany().HasForeignKey(l => l.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Yarn).WithMany().HasForeignKey(l => l.YarnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.PurchaseOrderLine).WithMany().HasForeignKey(l => l.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.LotNo).IsUnique();
        builder.HasIndex(l => new { l.YarnId, l.CustomerId });
    }
}
