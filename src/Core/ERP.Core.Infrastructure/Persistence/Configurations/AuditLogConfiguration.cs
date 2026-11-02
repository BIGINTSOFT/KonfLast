using ERP.Core.Domain.Entities;
using ERP.Core.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Core.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", t => t.HasCheckConstraint(
            "CK_AuditLogs_Action", UpperSnakeEnumConverter.CheckConstraintSql<AuditAction>("Action")));

        builder.Property(l => l.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(l => l.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(l => l.OldValues).HasColumnType("nvarchar(max)");
        builder.Property(l => l.NewValues).HasColumnType("nvarchar(max)");
        builder.Property(l => l.UserName).HasMaxLength(100).IsRequired();
        builder.Property(l => l.IpAddress).HasMaxLength(45);
        builder.Property(l => l.DeviceInfo).HasMaxLength(300);
        builder.Property(l => l.CorrelationId).HasMaxLength(64);

        // Büyüyecek bir tablo: "bir kaydın geçmişi", "bir kullanıcının işlemleri" ve
        // tarih aralığı sorguları index üzerinden çalışmalı.
        builder.HasIndex(l => new { l.EntityName, l.EntityId, l.Id });
        builder.HasIndex(l => new { l.UserName, l.Id });
        builder.HasIndex(l => l.CreatedAt);
    }
}
