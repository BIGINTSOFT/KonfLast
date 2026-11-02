using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ERP.Core.Infrastructure.Persistence.Configurations;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class DefectTypeConfiguration : IEntityTypeConfiguration<DefectType>
{
    /// <summary>Hazır gelen hata türleri. Id'ler sabittir; yenileri kullanıcı tarafından eklenir.</summary>
    private static readonly (int Id, string Code, string Name)[] Defaults =
    [
        (1, "DIKIS", "Dikiş hatası"),
        (2, "OLCU", "Ölçü hatası"),
        (3, "LEKE", "Leke"),
        (4, "SOKUK", "Sökük"),
        (5, "KUMAS", "Kumaş hatası"),
        (6, "YANLIS-RENK", "Yanlış renk"),
        (7, "YANLIS-BEDEN", "Yanlış beden"),
        (8, "DIGER", "Diğer"),
    ];

    public void Configure(EntityTypeBuilder<DefectType> builder)
    {
        builder.ToTable("DefectTypes");

        builder.Property(d => d.Code).HasMaxLength(30).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(d => d.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);

        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(Defaults.Select(d => new DefectType
        {
            Id = d.Id, Code = d.Code, Name = d.Name, IsActive = true, CreatedAt = seededAt, CreatedBy = "system",
        }));
    }
}

public sealed class QualityInspectionConfiguration : IEntityTypeConfiguration<QualityInspection>
{
    public void Configure(EntityTypeBuilder<QualityInspection> builder)
    {
        builder.ToTable("QualityInspections", t =>
        {
            t.HasCheckConstraint(
                "CK_QualityInspections_Result", UpperSnakeEnumConverter.CheckConstraintSql<QualityResult>("Result"));
            t.HasCheckConstraint(
                "CK_QualityInspections_Quantities",
                "[InspectedQuantity] > 0 AND [RejectedQuantity] >= 0 AND [ReworkQuantity] >= 0 " +
                "AND [RejectedQuantity] + [ReworkQuantity] <= [InspectedQuantity]");
        });

        builder.Ignore(i => i.PassedQuantity);
        builder.Property(i => i.Note).HasMaxLength(500);
        builder.Property(i => i.InspectedBy).HasMaxLength(100).IsRequired();

        builder.HasOne(i => i.Bundle).WithMany().HasForeignKey(i => i.BundleId);
        builder.HasOne(i => i.BundleOperation).WithMany().HasForeignKey(i => i.BundleOperationId);
        builder.HasMany(i => i.Records).WithOne(r => r.QualityInspection).HasForeignKey(r => r.QualityInspectionId);

        // Demetin kalite geçmişi; tarih ve sonuç bazlı listeler / raporlar.
        builder.HasIndex(i => new { i.BundleId, i.Id });
        builder.HasIndex(i => i.BundleOperationId);
        builder.HasIndex(i => new { i.InspectedAt, i.Result });
    }
}

public sealed class QualityRecordConfiguration : IEntityTypeConfiguration<QualityRecord>
{
    public void Configure(EntityTypeBuilder<QualityRecord> builder)
    {
        builder.ToTable("QualityRecords", t => t.HasCheckConstraint("CK_QualityRecords_Quantity", "[Quantity] > 0"));

        builder.HasOne(r => r.DefectType).WithMany().HasForeignKey(r => r.DefectTypeId);

        builder.HasIndex(r => r.QualityInspectionId);
        builder.HasIndex(r => r.DefectTypeId);
    }
}

public sealed class ReworkTaskConfiguration : IEntityTypeConfiguration<ReworkTask>
{
    public void Configure(EntityTypeBuilder<ReworkTask> builder)
    {
        builder.ToTable("ReworkTasks", t =>
        {
            t.HasCheckConstraint("CK_ReworkTasks_Status", UpperSnakeEnumConverter.CheckConstraintSql<ReworkStatus>("Status"));
            t.HasCheckConstraint("CK_ReworkTasks_Quantity", "[Quantity] > 0");
        });

        builder.Property(t => t.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(t => t.ClosedBy).HasMaxLength(100);

        builder.HasOne(t => t.QualityInspection).WithMany().HasForeignKey(t => t.QualityInspectionId);
        builder.HasOne(t => t.Bundle).WithMany().HasForeignKey(t => t.BundleId);
        builder.HasOne(t => t.BundleOperation).WithMany().HasForeignKey(t => t.BundleOperationId);

        builder.HasIndex(t => new { t.Status, t.Id });
        builder.HasIndex(t => new { t.BundleId, t.Id });
        builder.HasIndex(t => new { t.BundleOperationId, t.Status });
        builder.HasIndex(t => t.QualityInspectionId);
    }
}
