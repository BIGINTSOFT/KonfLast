using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneDyeChemicalConfiguration : IEntityTypeConfiguration<BoyahaneDyeChemical>
{
    public void Configure(EntityTypeBuilder<BoyahaneDyeChemical> builder)
    {
        builder.Property(c => c.MinStockKilograms).HasPrecision(14, 3);
        builder.ToTable("DyeChemicals", t =>
            t.HasCheckConstraint("CK_DyeChemicals_Kind", UpperSnakeEnumConverter.CheckConstraintSql<DyeChemicalKind>("Kind")));

        builder.Property(d => d.Code).HasMaxLength(30).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();

        builder.HasIndex(d => d.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class BoyahaneLabRequestConfiguration : IEntityTypeConfiguration<BoyahaneLabRequest>
{
    public void Configure(EntityTypeBuilder<BoyahaneLabRequest> builder)
    {
        builder.ToTable("LabRequests", t =>
            t.HasCheckConstraint("CK_LabRequests_Status", UpperSnakeEnumConverter.CheckConstraintSql<LabRequestStatus>("Status")));

        builder.Property(r => r.RequestNo).HasMaxLength(30).IsRequired();
        builder.Property(r => r.TargetNote).HasMaxLength(500);

        builder.HasOne(r => r.OrderLine).WithMany().HasForeignKey(r => r.OrderLineId);

        builder.HasIndex(r => r.RequestNo).IsUnique();

        // Satırda en fazla bir etkin talep olur; iptal edilenler geçmişte kalır.
        builder.HasIndex(r => r.OrderLineId, "UX_LabRequests_ActivePerLine").IsUnique().HasFilter("[Status] <> 'CANCELLED'");
    }
}

public sealed class BoyahaneLabTrialConfiguration : IEntityTypeConfiguration<BoyahaneLabTrial>
{
    public void Configure(EntityTypeBuilder<BoyahaneLabTrial> builder)
    {
        builder.ToTable("LabTrials", t =>
            t.HasCheckConstraint("CK_LabTrials_Decision", UpperSnakeEnumConverter.CheckConstraintSql<LabTrialDecision>("Decision")));

        builder.Property(t => t.TrialCode).HasMaxLength(2).IsRequired();
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.Property(t => t.DecisionBy).HasMaxLength(100);
        builder.Property(t => t.DecisionNote).HasMaxLength(500);
        builder.Property(t => t.DecisionRecordedBy).HasMaxLength(100);

        builder.HasOne(t => t.Request).WithMany(r => r.Trials).HasForeignKey(t => t.RequestId);
        builder.HasOne(t => t.ReusedFromTrial).WithMany().HasForeignKey(t => t.ReusedFromTrialId);

        builder.HasIndex(t => new { t.RequestId, t.TrialCode }).IsUnique();
    }
}

public sealed class BoyahaneLabTrialItemConfiguration : IEntityTypeConfiguration<BoyahaneLabTrialItem>
{
    public void Configure(EntityTypeBuilder<BoyahaneLabTrialItem> builder)
    {
        builder.ToTable("LabTrialItems", t => t.HasCheckConstraint("CK_LabTrialItems_Amount", "[Amount] > 0"));

        builder.Property(i => i.Amount).HasPrecision(10, 4);

        builder.HasOne(i => i.Trial).WithMany(t => t.Items).HasForeignKey(i => i.TrialId);
        builder.HasOne(i => i.DyeChemical).WithMany().HasForeignKey(i => i.DyeChemicalId);

        builder.HasIndex(i => new { i.TrialId, i.DyeChemicalId }).IsUnique();
    }
}
