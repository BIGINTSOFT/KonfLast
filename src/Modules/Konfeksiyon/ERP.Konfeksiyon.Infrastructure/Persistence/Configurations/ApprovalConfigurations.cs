using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class ApprovalFlowConfiguration : IEntityTypeConfiguration<ApprovalFlow>
{
    public void Configure(EntityTypeBuilder<ApprovalFlow> builder)
    {
        builder.ToTable("ApprovalFlows", t => t.HasCheckConstraint(
            "CK_ApprovalFlows_DocumentType", UpperSnakeEnumConverter.CheckConstraintSql<ApprovalDocumentType>("DocumentType")));

        builder.Property(f => f.Name).HasMaxLength(100).IsRequired();

        // Belge türü başına tek akış.
        builder.HasIndex(f => f.DocumentType).IsUnique();
    }
}

public sealed class ApprovalFlowStepConfiguration : IEntityTypeConfiguration<ApprovalFlowStep>
{
    public void Configure(EntityTypeBuilder<ApprovalFlowStep> builder)
    {
        builder.ToTable("ApprovalFlowSteps", t =>
        {
            t.HasCheckConstraint("CK_ApprovalFlowSteps_ApproverType", UpperSnakeEnumConverter.CheckConstraintSql<ApproverType>("ApproverType"));
            t.HasCheckConstraint("CK_ApprovalFlowSteps_Role", "[ApproverType] <> 'ROLE' OR [RoleId] IS NOT NULL");
            t.HasCheckConstraint("CK_ApprovalFlowSteps_MinAmount", "[MinAmount] IS NULL OR [MinAmount] >= 0");
        });

        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();

        builder.HasOne(s => s.ApprovalFlow).WithMany(f => f.Steps).HasForeignKey(s => s.ApprovalFlowId);
        builder.HasOne(s => s.Role).WithMany().HasForeignKey(s => s.RoleId);

        builder.HasIndex(s => new { s.ApprovalFlowId, s.Sequence }).IsUnique();
    }
}

public sealed class ApprovalFlowStepUserConfiguration : IEntityTypeConfiguration<ApprovalFlowStepUser>
{
    public void Configure(EntityTypeBuilder<ApprovalFlowStepUser> builder)
    {
        builder.ToTable("ApprovalFlowStepUsers");
        builder.HasKey(u => new { u.ApprovalFlowStepId, u.UserId });

        builder.HasOne(u => u.ApprovalFlowStep).WithMany(s => s.Users).HasForeignKey(u => u.ApprovalFlowStepId);
        builder.HasOne(u => u.User).WithMany().HasForeignKey(u => u.UserId);
    }
}

public sealed class SalesOrderApprovalStepConfiguration : IEntityTypeConfiguration<SalesOrderApprovalStep>
{
    public void Configure(EntityTypeBuilder<SalesOrderApprovalStep> builder)
    {
        builder.ToTable("SalesOrderApprovalSteps", t =>
        {
            t.HasCheckConstraint(
                "CK_SalesOrderApprovalSteps_ApproverType", UpperSnakeEnumConverter.CheckConstraintSql<ApproverType>("ApproverType"));
            t.HasCheckConstraint(
                "CK_SalesOrderApprovalSteps_Status", UpperSnakeEnumConverter.CheckConstraintSql<ApprovalStepStatus>("Status"));
        });

        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.ApproverUserIds).HasMaxLength(1000).IsUnicode(false);
        builder.Property(s => s.ApproverNames).HasMaxLength(1000).IsRequired();
        builder.Property(s => s.DecidedBy).HasMaxLength(100);
        builder.Property(s => s.Note).HasMaxLength(500);

        builder.HasOne(s => s.SalesOrder).WithMany().HasForeignKey(s => s.SalesOrderId);

        builder.HasIndex(s => new { s.SalesOrderId, s.Round, s.Sequence }).IsUnique();
        // "Onayımı bekleyenler" sorgusu şu anki adımları tarar.
        builder.HasIndex(s => s.Status).HasFilter("[Status] = 'PENDING'");
    }
}

public sealed class SalesOrderApprovalEventConfiguration : IEntityTypeConfiguration<SalesOrderApprovalEvent>
{
    public void Configure(EntityTypeBuilder<SalesOrderApprovalEvent> builder)
    {
        builder.ToTable("SalesOrderApprovalEvents", t => t.HasCheckConstraint(
            "CK_SalesOrderApprovalEvents_EventType", UpperSnakeEnumConverter.CheckConstraintSql<ApprovalEventType>("EventType")));

        builder.Property(e => e.StepName).HasMaxLength(100);
        builder.Property(e => e.UserName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.FullName).HasMaxLength(200);
        builder.Property(e => e.Note).HasMaxLength(500);

        builder.HasOne(e => e.SalesOrder).WithMany().HasForeignKey(e => e.SalesOrderId);

        builder.HasIndex(e => new { e.SalesOrderId, e.CreatedAt });
    }
}
