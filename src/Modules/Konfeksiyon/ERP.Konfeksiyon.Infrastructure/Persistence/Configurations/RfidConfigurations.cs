using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ERP.Core.Infrastructure.Persistence.Configurations;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class RfidCardConfiguration : IEntityTypeConfiguration<RfidCard>
{
    public void Configure(EntityTypeBuilder<RfidCard> builder)
    {
        builder.ToTable("RfidCards", t => t.HasCheckConstraint(
            "CK_RfidCards_CardType", UpperSnakeEnumConverter.CheckConstraintSql<RfidCardType>("CardType")));

        builder.Property(c => c.Uid).HasMaxLength(RfidUid.MaxLength).IsUnicode(false).IsRequired();
        builder.Property(c => c.Label).HasMaxLength(50);

        // Her okutmada UID ile arama yapılır.
        builder.HasIndex(c => c.Uid).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class BundleCardAssignmentConfiguration : IEntityTypeConfiguration<BundleCardAssignment>
{
    public void Configure(EntityTypeBuilder<BundleCardAssignment> builder)
    {
        builder.ToTable("BundleCardAssignments", t => t.HasCheckConstraint(
            "CK_BundleCardAssignments_Period", "[ReleasedAt] IS NULL OR [ReleasedAt] >= [AssignedAt]"));

        builder.HasOne(a => a.RfidCard).WithMany().HasForeignKey(a => a.RfidCardId);
        builder.HasOne(a => a.Bundle).WithMany().HasForeignKey(a => a.BundleId);

        // Bir kart aynı anda tek demette, bir demette aynı anda tek kart olabilir.
        builder.HasIndex(a => a.RfidCardId)
            .IsUnique()
            .HasDatabaseName("UX_BundleCardAssignments_CurrentPerCard")
            .HasFilter("[ReleasedAt] IS NULL");
        builder.HasIndex(a => a.BundleId)
            .IsUnique()
            .HasDatabaseName("UX_BundleCardAssignments_CurrentPerBundle")
            .HasFilter("[ReleasedAt] IS NULL");

        builder.HasIndex(a => new { a.RfidCardId, a.AssignedAt });
        builder.HasIndex(a => new { a.BundleId, a.AssignedAt });
    }
}

public sealed class OperatorCardConfiguration : IEntityTypeConfiguration<OperatorCard>
{
    public void Configure(EntityTypeBuilder<OperatorCard> builder)
    {
        builder.ToTable("OperatorCards", t => t.HasCheckConstraint(
            "CK_OperatorCards_Period", "[RevokedAt] IS NULL OR [RevokedAt] >= [AssignedAt]"));

        builder.HasOne(c => c.Operator).WithMany().HasForeignKey(c => c.OperatorId);
        builder.HasOne(c => c.RfidCard).WithMany().HasForeignKey(c => c.RfidCardId);

        // Bir kart tek operatöre, bir operatörün aynı anda tek geçerli kartı.
        builder.HasIndex(c => c.RfidCardId)
            .IsUnique()
            .HasDatabaseName("UX_OperatorCards_CurrentPerCard")
            .HasFilter("[RevokedAt] IS NULL");
        builder.HasIndex(c => c.OperatorId)
            .IsUnique()
            .HasDatabaseName("UX_OperatorCards_CurrentPerOperator")
            .HasFilter("[RevokedAt] IS NULL");

        builder.HasIndex(c => new { c.OperatorId, c.AssignedAt });
    }
}

public sealed class RfidReadLogConfiguration : IEntityTypeConfiguration<RfidReadLog>
{
    public void Configure(EntityTypeBuilder<RfidReadLog> builder)
    {
        builder.ToTable("RfidReadLogs", t => t.HasCheckConstraint(
            "CK_RfidReadLogs_Kind", UpperSnakeEnumConverter.CheckConstraintSql<RfidReadKind>("Kind")));

        builder.Property(l => l.Uid).HasMaxLength(RfidUid.MaxLength).IsUnicode(false).IsRequired();
        builder.Property(l => l.ResultCode).HasMaxLength(50).IsUnicode(false).IsRequired();

        // Idempotency: aynı okuma kimliği ikinci kez kaydedilemez.
        builder.HasIndex(l => l.ReadId).IsUnique();

        // Debounce ("bu tablet bu kartı az önce okudu mu?") ve tablet / kart bazlı geçmiş sorguları.
        builder.HasIndex(l => new { l.TabletId, l.Uid, l.Kind, l.ReadAt });
        builder.HasIndex(l => new { l.Uid, l.ReadAt });
        builder.HasIndex(l => l.ReadAt);
    }
}
