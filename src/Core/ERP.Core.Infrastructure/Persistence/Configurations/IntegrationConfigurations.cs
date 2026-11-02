using ERP.Core.Domain.Entities;
using ERP.Core.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Core.Infrastructure.Persistence.Configurations;

public sealed class IntegrationMessageConfiguration : IEntityTypeConfiguration<IntegrationMessage>
{
    public void Configure(EntityTypeBuilder<IntegrationMessage> builder)
    {
        builder.ToTable("IntegrationMessages");

        builder.Property(m => m.EventType).HasMaxLength(100).IsRequired();
        builder.Property(m => m.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.EntityId).HasMaxLength(50).IsRequired();
        builder.Property(m => m.Reference).HasMaxLength(50);
        builder.Property(m => m.Payload).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(m => m.CreatedBy).HasMaxLength(100).IsRequired();

        builder.HasIndex(m => m.EventId).IsUnique();

        // Tablo sürekli büyür: tür filtresi, bir kaydın olayları ve numarayla arama index üzerinden çalışmalı.
        builder.HasIndex(m => new { m.EventType, m.Id });
        builder.HasIndex(m => new { m.EntityName, m.EntityId });
        builder.HasIndex(m => m.Reference);
    }
}

public sealed class IntegrationDeliveryConfiguration : IEntityTypeConfiguration<IntegrationDelivery>
{
    public void Configure(EntityTypeBuilder<IntegrationDelivery> builder)
    {
        builder.ToTable("IntegrationDeliveries", t =>
        {
            t.HasCheckConstraint(
                "CK_IntegrationDeliveries_Status", UpperSnakeEnumConverter.CheckConstraintSql<IntegrationDeliveryStatus>("Status"));
            t.HasCheckConstraint("CK_IntegrationDeliveries_AttemptCount", "[AttemptCount] >= 0");

            // Başarılı teslimin ne zaman ulaştığı bellidir.
            t.HasCheckConstraint(
                "CK_IntegrationDeliveries_Delivered", "[Status] <> 'SUCCEEDED' OR [DeliveredAt] IS NOT NULL");
        });

        builder.Property(d => d.Channel).HasMaxLength(100).IsRequired();
        builder.Property(d => d.LastError).HasMaxLength(1000);

        builder.HasOne(d => d.Message).WithMany(m => m.Deliveries).HasForeignKey(d => d.IntegrationMessageId);

        // Bir olay bir kanala tek teslimdir.
        builder.HasIndex(d => new { d.IntegrationMessageId, d.Channel }).IsUnique();

        // Gönderici "bekleyen ve zamanı gelmiş" teslimleri birkaç saniyede bir arar.
        builder.HasIndex(d => new { d.Status, d.NextAttemptAt });
        builder.HasIndex(d => new { d.Channel, d.Status });
    }
}
