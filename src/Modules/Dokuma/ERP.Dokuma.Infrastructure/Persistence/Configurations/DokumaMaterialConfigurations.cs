using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Dokuma.Infrastructure.Persistence.Configurations;

public sealed class DokumaSupplierConfiguration : IEntityTypeConfiguration<DokumaSupplier>
{
    public void Configure(EntityTypeBuilder<DokumaSupplier> builder)
    {
        builder.ToTable("Suppliers", t => t.HasCheckConstraint("CK_Suppliers_LeadTimeDays", "[LeadTimeDays] IS NULL OR [LeadTimeDays] >= 0"));
        builder.Property(s => s.Code).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.TaxNumber).HasMaxLength(20);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(150);
        builder.Property(s => s.Currency).HasColumnType("char(3)").HasDefaultValue("TRY").IsRequired();
        builder.HasIndex(s => s.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class DokumaYarnConfiguration : IEntityTypeConfiguration<DokumaYarn>
{
    public void Configure(EntityTypeBuilder<DokumaYarn> builder)
    {
        builder.ToTable("Yarns", t =>
        {
            t.HasCheckConstraint("CK_Yarns_CountSystem", UpperSnakeEnumConverter.CheckConstraintSql<YarnCountSystem>("CountSystem"));
            t.HasCheckConstraint("CK_Yarns_CountValue", "[CountValue] > 0");
            t.HasCheckConstraint("CK_Yarns_Plies", "[Plies] >= 1");
        });

        builder.Property(y => y.YarnCode).HasMaxLength(30).IsRequired();
        builder.Property(y => y.Name).HasMaxLength(200).IsRequired();
        builder.Property(y => y.CountValue).HasPrecision(8, 2);
        builder.Property(y => y.Composition).HasMaxLength(200);
        builder.Property(y => y.Twist).HasMaxLength(50);
        builder.Property(y => y.Color).HasMaxLength(100);

        builder.HasIndex(y => y.YarnCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class DokumaLoomConfiguration : IEntityTypeConfiguration<DokumaLoom>
{
    public void Configure(EntityTypeBuilder<DokumaLoom> builder)
    {
        builder.ToTable("Looms", t =>
        {
            t.HasCheckConstraint("CK_Looms_LoomType", UpperSnakeEnumConverter.CheckConstraintSql<LoomType>("LoomType"));
            t.HasCheckConstraint("CK_Looms_SheddingType", UpperSnakeEnumConverter.CheckConstraintSql<SheddingType>("SheddingType"));
            t.HasCheckConstraint("CK_Looms_MaxReedWidthCm", "[MaxReedWidthCm] IS NULL OR [MaxReedWidthCm] > 0");
            t.HasCheckConstraint("CK_Looms_NominalSpeedRpm", "[NominalSpeedRpm] IS NULL OR [NominalSpeedRpm] > 0");
        });

        builder.Property(l => l.Code).HasMaxLength(30).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(200).IsRequired();
        builder.Property(l => l.MaxReedWidthCm).HasPrecision(6, 2);
        builder.Property(l => l.Brand).HasMaxLength(100);

        builder.HasIndex(l => l.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class DokumaArticleConfiguration : IEntityTypeConfiguration<DokumaArticle>
{
    public void Configure(EntityTypeBuilder<DokumaArticle> builder)
    {
        builder.ToTable("Articles", t =>
        {
            t.HasCheckConstraint("CK_Articles_SheddingType", UpperSnakeEnumConverter.CheckConstraintSql<SheddingType>("SheddingType"));
            t.HasCheckConstraint("CK_Articles_ReedNumber", "[ReedNumber] IS NULL OR [ReedNumber] > 0");
            t.HasCheckConstraint("CK_Articles_EndsPerDent", "[EndsPerDent] IS NULL OR [EndsPerDent] >= 1");
            t.HasCheckConstraint("CK_Articles_ReedWidthCm", "[ReedWidthCm] > 0");
            t.HasCheckConstraint("CK_Articles_WeftDensityPerCm", "[WeftDensityPerCm] > 0");
            t.HasCheckConstraint("CK_Articles_RawWidthCm", "[RawWidthCm] > 0");
            t.HasCheckConstraint("CK_Articles_RawWeightGsm", "[RawWeightGsm] > 0");
            t.HasCheckConstraint("CK_Articles_WarpCrimpPercent", "[WarpCrimpPercent] IS NULL OR [WarpCrimpPercent] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_Articles_WeftCrimpPercent", "[WeftCrimpPercent] IS NULL OR [WeftCrimpPercent] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_Articles_WarpWastePercent", "[WarpWastePercent] IS NULL OR [WarpWastePercent] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_Articles_WeftWastePercent", "[WeftWastePercent] IS NULL OR [WeftWastePercent] BETWEEN 0 AND 100");
        });

        builder.Property(a => a.ArticleCode).HasMaxLength(30).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.WeaveName).HasMaxLength(100);
        builder.Property(a => a.Composition).HasMaxLength(200);
        builder.Property(a => a.ReedNumber).HasPrecision(8, 2);
        builder.Property(a => a.ReedWidthCm).HasPrecision(6, 2);
        builder.Property(a => a.WeftDensityPerCm).HasPrecision(6, 2);
        builder.Property(a => a.RawWidthCm).HasPrecision(6, 2);
        builder.Property(a => a.WarpCrimpPercent).HasPrecision(5, 2);
        builder.Property(a => a.WeftCrimpPercent).HasPrecision(5, 2);
        builder.Property(a => a.WarpWastePercent).HasPrecision(5, 2);
        builder.Property(a => a.WeftWastePercent).HasPrecision(5, 2);

        builder.HasIndex(a => a.ArticleCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class DokumaArticleYarnConfiguration : IEntityTypeConfiguration<DokumaArticleYarn>
{
    public void Configure(EntityTypeBuilder<DokumaArticleYarn> builder)
    {
        builder.ToTable("ArticleYarns", t =>
        {
            t.HasCheckConstraint("CK_ArticleYarns_Role", UpperSnakeEnumConverter.CheckConstraintSql<ArticleYarnRole>("Role"));
            t.HasCheckConstraint("CK_ArticleYarns_Sequence", "[Sequence] >= 1");
            t.HasCheckConstraint("CK_ArticleYarns_Ends", "[Ends] >= 1");
        });

        builder.HasOne(y => y.Article).WithMany(a => a.Yarns).HasForeignKey(y => y.ArticleId);
        builder.HasOne(y => y.Yarn).WithMany().HasForeignKey(y => y.YarnId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(y => new { y.ArticleId, y.Role, y.Sequence }).IsUnique();
    }
}
