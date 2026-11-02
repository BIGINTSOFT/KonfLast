using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class SubcontractPriceConfiguration : IEntityTypeConfiguration<SubcontractPrice>
{
    public void Configure(EntityTypeBuilder<SubcontractPrice> builder)
    {
        builder.ToTable("SubcontractPrices", t => t.HasCheckConstraint("CK_SubcontractPrices_UnitPrice", "[UnitPrice] >= 0"));

        builder.Property(p => p.UnitPrice).HasPrecision(18, 4);
        builder.Property(p => p.Currency).HasColumnType("char(3)").IsRequired();
        builder.Property(p => p.Notes).HasMaxLength(500);

        builder.HasOne(p => p.Supplier).WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Operation).WithMany().HasForeignKey(p => p.OperationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Product).WithMany().HasForeignKey(p => p.ProductId).OnDelete(DeleteBehavior.Restrict);

        // Fasoncu + operasyon + ürün (boş ürün de bir değerdir) için bir başlangıç gününde tek fiyat.
        builder.HasIndex(p => new { p.SupplierId, p.OperationId, p.ProductId, p.ValidFrom }).IsUnique().HasFilter(null);
        builder.HasIndex(p => p.OperationId);
        builder.HasIndex(p => p.ProductId);
    }
}

public sealed class SubcontractSendConfiguration : IEntityTypeConfiguration<SubcontractSend>
{
    public void Configure(EntityTypeBuilder<SubcontractSend> builder)
    {
        builder.ToTable("SubcontractSends", t =>
            t.HasCheckConstraint("CK_SubcontractSends_Status", UpperSnakeEnumConverter.CheckConstraintSql<SubcontractSendStatus>("Status")));

        builder.Property(s => s.SendNo).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Carrier).HasMaxLength(200);
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.Ignore(s => s.IsOpen);
        builder.Ignore(s => s.CanCancel);
        builder.Ignore(s => s.TotalQuantity);
        builder.Ignore(s => s.OutstandingQuantity);

        builder.HasOne(s => s.Supplier).WithMany().HasForeignKey(s => s.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Operation).WithMany().HasForeignKey(s => s.OperationId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.SendNo).IsUnique();
        builder.HasIndex(s => new { s.SupplierId, s.Status });
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.OperationId);
    }
}

public sealed class SubcontractSendLineConfiguration : IEntityTypeConfiguration<SubcontractSendLine>
{
    public void Configure(EntityTypeBuilder<SubcontractSendLine> builder)
    {
        builder.ToTable("SubcontractSendLines", t =>
        {
            t.HasCheckConstraint("CK_SubcontractSendLines_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint(
                "CK_SubcontractSendLines_Returned",
                "[ReturnedGood] >= 0 AND [ReturnedDefective] >= 0 AND [ReturnedMissing] >= 0 " +
                "AND [ReturnedGood] + [ReturnedDefective] + [ReturnedMissing] <= [Quantity]");
            t.HasCheckConstraint("CK_SubcontractSendLines_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
        });

        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.Currency).HasColumnType("char(3)").IsRequired();

        builder.Ignore(l => l.ReturnedTotal);
        builder.Ignore(l => l.Outstanding);

        builder.HasOne(l => l.SubcontractSend).WithMany(s => s.Lines).HasForeignKey(l => l.SubcontractSendId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.Bundle).WithMany().HasForeignKey(l => l.BundleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.BundleOperation).WithMany().HasForeignKey(l => l.BundleOperationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.ProductionOrder).WithMany().HasForeignKey(l => l.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Color).WithMany().HasForeignKey(l => l.ColorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Size).WithMany().HasForeignKey(l => l.SizeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.SubcontractSendId);
        builder.HasIndex(l => l.BundleId);
        builder.HasIndex(l => l.BundleOperationId);
        builder.HasIndex(l => l.ProductionOrderId);
        builder.HasIndex(l => l.ProductId);
        builder.HasIndex(l => l.ColorId);
        builder.HasIndex(l => l.SizeId);
    }
}

public sealed class SubcontractReturnConfiguration : IEntityTypeConfiguration<SubcontractReturn>
{
    public void Configure(EntityTypeBuilder<SubcontractReturn> builder)
    {
        builder.ToTable("SubcontractReturns");

        builder.Property(r => r.ReturnNo).HasMaxLength(30).IsRequired();
        builder.Property(r => r.SupplierDocumentNo).HasMaxLength(50);
        builder.Property(r => r.Notes).HasMaxLength(1000);
        builder.Property(r => r.PaidBy).HasMaxLength(100);
        builder.Property(r => r.PaymentNote).HasMaxLength(500);
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.Ignore(r => r.IsPaid);

        builder.HasOne(r => r.SubcontractSend).WithMany().HasForeignKey(r => r.SubcontractSendId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Supplier).WithMany().HasForeignKey(r => r.SupplierId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.ReturnNo).IsUnique();
        builder.HasIndex(r => r.SubcontractSendId);

        // Hakediş listesi fasoncu ve döneme göre okunur.
        builder.HasIndex(r => new { r.SupplierId, r.ReturnDate });
        builder.HasIndex(r => r.ReturnDate);
    }
}

public sealed class SubcontractReturnLineConfiguration : IEntityTypeConfiguration<SubcontractReturnLine>
{
    public void Configure(EntityTypeBuilder<SubcontractReturnLine> builder)
    {
        builder.ToTable("SubcontractReturnLines", t =>
        {
            t.HasCheckConstraint(
                "CK_SubcontractReturnLines_Quantities",
                "[GoodQuantity] >= 0 AND [DefectiveQuantity] >= 0 AND [MissingQuantity] >= 0 " +
                "AND [GoodQuantity] + [DefectiveQuantity] + [MissingQuantity] > 0");
            t.HasCheckConstraint("CK_SubcontractReturnLines_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
        });

        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.Currency).HasColumnType("char(3)").IsRequired();

        builder.Ignore(l => l.Amount);

        builder.HasOne(l => l.SubcontractReturn).WithMany(r => r.Lines).HasForeignKey(l => l.SubcontractReturnId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.SubcontractSendLine).WithMany().HasForeignKey(l => l.SubcontractSendLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.DefectType).WithMany().HasForeignKey(l => l.DefectTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.SubcontractReturnId);
        builder.HasIndex(l => l.SubcontractSendLineId);
        builder.HasIndex(l => l.DefectTypeId);
    }
}
