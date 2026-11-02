using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneDefectTypeConfiguration : IEntityTypeConfiguration<BoyahaneDefectType>
{
    public void Configure(EntityTypeBuilder<BoyahaneDefectType> builder)
    {
        builder.ToTable("DefectTypes", t =>
            t.HasCheckConstraint("CK_DefectTypes_Category", UpperSnakeEnumConverter.CheckConstraintSql<DefectCategory>("Category")));

        builder.Property(d => d.Code).HasMaxLength(30).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(d => d.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class BoyahaneFabricRollDefectConfiguration : IEntityTypeConfiguration<BoyahaneFabricRollDefect>
{
    public void Configure(EntityTypeBuilder<BoyahaneFabricRollDefect> builder)
    {
        builder.ToTable("FabricRollDefects", t => t.HasCheckConstraint("CK_FabricRollDefects_Quantity", "[Quantity] >= 1"));

        builder.Property(d => d.AtMeter).HasPrecision(12, 2);
        builder.Property(d => d.Note).HasMaxLength(200);

        builder.HasOne(d => d.Roll).WithMany(t => t.Defects).HasForeignKey(d => d.RollId);
        builder.HasOne(d => d.DefectType).WithMany().HasForeignKey(d => d.DefectTypeId);
    }
}
