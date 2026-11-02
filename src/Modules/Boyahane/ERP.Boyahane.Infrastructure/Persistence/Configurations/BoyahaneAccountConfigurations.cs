using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahanePaymentConfiguration : IEntityTypeConfiguration<BoyahanePayment>
{
    public void Configure(EntityTypeBuilder<BoyahanePayment> builder)
    {
        builder.ToTable("Payments", t =>
        {
            t.HasCheckConstraint("CK_Payments_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_Payments_Method", UpperSnakeEnumConverter.CheckConstraintSql<PaymentMethod>("Method"));
            t.HasCheckConstraint("CK_Payments_Status", UpperSnakeEnumConverter.CheckConstraintSql<PaymentStatus>("Status"));
        });
        builder.Property(p => p.PaymentNo).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(16, 2);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.ReferenceNo).HasMaxLength(50);
        builder.Property(p => p.Notes).HasMaxLength(500);
        builder.Property(p => p.CancelReason).HasMaxLength(500);
        builder.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.PaymentNo).IsUnique();
        builder.HasIndex(p => new { p.CustomerId, p.Currency, p.PaymentDate });
    }
}

public sealed class BoyahanePaymentAllocationConfiguration : IEntityTypeConfiguration<BoyahanePaymentAllocation>
{
    public void Configure(EntityTypeBuilder<BoyahanePaymentAllocation> builder)
    {
        builder.ToTable("PaymentAllocations", t => t.HasCheckConstraint("CK_PaymentAllocations_Amount", "[Amount] > 0"));
        builder.Property(a => a.Amount).HasPrecision(16, 2);
        builder.HasOne(a => a.Payment).WithMany(p => p.Allocations).HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Invoice).WithMany().HasForeignKey(a => a.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.PaymentId, a.InvoiceId }).IsUnique();
        builder.HasIndex(a => a.InvoiceId);
    }
}
