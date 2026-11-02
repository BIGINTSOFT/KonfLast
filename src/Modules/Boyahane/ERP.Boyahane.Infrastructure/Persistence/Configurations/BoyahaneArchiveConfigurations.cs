using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahaneColorCardConfiguration : IEntityTypeConfiguration<BoyahaneColorCard>
{
    public void Configure(EntityTypeBuilder<BoyahaneColorCard> builder)
    {
        builder.ToTable("ColorCards");
        builder.Property(c => c.Code).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.CustomerColorCode).HasMaxLength(50);
        builder.Property(c => c.Pantone).HasMaxLength(50);
        builder.Property(c => c.LabL).HasPrecision(7, 3);
        builder.Property(c => c.LabA).HasPrecision(7, 3);
        builder.Property(c => c.LabB).HasPrecision(7, 3);
        builder.Property(c => c.Notes).HasMaxLength(500);

        builder.HasOne(c => c.Customer).WithMany().HasForeignKey(c => c.CustomerId);

        builder.HasIndex(c => c.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
        builder.HasIndex(c => c.CustomerId);
    }
}

public sealed class BoyahaneColorCardImageConfiguration : IEntityTypeConfiguration<BoyahaneColorCardImage>
{
    public void Configure(EntityTypeBuilder<BoyahaneColorCardImage> builder)
    {
        builder.ToTable("ColorCardImages");
        builder.HasKey(i => i.ColorCardId);
        builder.Property(i => i.Content).IsRequired();
        builder.Property(i => i.ContentType).HasMaxLength(20).IsRequired();
        builder.HasOne(i => i.ColorCard).WithOne().HasForeignKey<BoyahaneColorCardImage>(i => i.ColorCardId);
    }
}

public sealed class BoyahanePatternConfiguration : IEntityTypeConfiguration<BoyahanePattern>
{
    public void Configure(EntityTypeBuilder<BoyahanePattern> builder)
    {
        builder.ToTable("Patterns", t =>
        {
            t.HasCheckConstraint("CK_Patterns_PrintType", UpperSnakeEnumConverter.CheckConstraintSql<PrintType>("PrintType"));
            t.HasCheckConstraint("CK_Patterns_ScreenCount", "[ScreenCount] IS NULL OR [ScreenCount] > 0");
        });
        builder.Property(p => p.Code).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.RepeatWidthCm).HasPrecision(8, 2);
        builder.Property(p => p.RepeatHeightCm).HasPrecision(8, 2);
        builder.Property(p => p.Notes).HasMaxLength(500);

        builder.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId);

        builder.HasIndex(p => p.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
        builder.HasIndex(p => p.CustomerId);
    }
}

public sealed class BoyahanePatternImageConfiguration : IEntityTypeConfiguration<BoyahanePatternImage>
{
    public void Configure(EntityTypeBuilder<BoyahanePatternImage> builder)
    {
        builder.ToTable("PatternImages");
        builder.HasKey(i => i.PatternId);
        builder.Property(i => i.Content).IsRequired();
        builder.Property(i => i.ContentType).HasMaxLength(20).IsRequired();
        builder.HasOne(i => i.Pattern).WithOne().HasForeignKey<BoyahanePatternImage>(i => i.PatternId);
    }
}

public sealed class BoyahanePatternColorwayConfiguration : IEntityTypeConfiguration<BoyahanePatternColorway>
{
    public void Configure(EntityTypeBuilder<BoyahanePatternColorway> builder)
    {
        builder.ToTable("PatternColorways");
        builder.Property(c => c.Code).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();

        builder.HasOne(c => c.Pattern).WithMany(p => p.Colorways).HasForeignKey(c => c.PatternId);

        builder.HasIndex(c => new { c.PatternId, c.Code }).IsUnique();
    }
}

public sealed class BoyahaneColorwayColorConfiguration : IEntityTypeConfiguration<BoyahaneColorwayColor>
{
    public void Configure(EntityTypeBuilder<BoyahaneColorwayColor> builder)
    {
        builder.ToTable("ColorwayColors", t =>
        {
            t.HasCheckConstraint("CK_ColorwayColors_Position", "[Position] > 0");
            t.HasCheckConstraint("CK_ColorwayColors_Paste", "[PasteKilogramsPerMeter] IS NULL OR [PasteKilogramsPerMeter] > 0");
        });
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.PasteKilogramsPerMeter).HasPrecision(10, 4);

        builder.HasOne(c => c.Colorway).WithMany(w => w.Colors).HasForeignKey(c => c.ColorwayId);
        builder.HasOne(c => c.ColorCard).WithMany().HasForeignKey(c => c.ColorCardId);

        builder.HasIndex(c => new { c.ColorwayId, c.Position }).IsUnique();
    }
}

public sealed class BoyahanePrintRecipeLineConfiguration : IEntityTypeConfiguration<BoyahanePrintRecipeLine>
{
    public void Configure(EntityTypeBuilder<BoyahanePrintRecipeLine> builder)
    {
        builder.ToTable("PrintRecipeLines", t => t.HasCheckConstraint("CK_PrintRecipeLines_Amount", "[GramsPerKilogram] > 0"));
        builder.Property(l => l.GramsPerKilogram).HasPrecision(10, 4);

        builder.HasOne(l => l.ColorwayColor).WithMany(c => c.Lines).HasForeignKey(l => l.ColorwayColorId);
        builder.HasOne(l => l.DyeChemical).WithMany().HasForeignKey(l => l.DyeChemicalId);

        builder.HasIndex(l => new { l.ColorwayColorId, l.DyeChemicalId }).IsUnique();
    }
}

public sealed class BoyahaneStrippingRecipeConfiguration : IEntityTypeConfiguration<BoyahaneStrippingRecipe>
{
    public void Configure(EntityTypeBuilder<BoyahaneStrippingRecipe> builder)
    {
        builder.ToTable("StrippingRecipes");
        builder.Property(r => r.Code).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(500);

        builder.HasIndex(r => r.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}

public sealed class BoyahaneStrippingRecipeLineConfiguration : IEntityTypeConfiguration<BoyahaneStrippingRecipeLine>
{
    public void Configure(EntityTypeBuilder<BoyahaneStrippingRecipeLine> builder)
    {
        builder.ToTable("StrippingRecipeLines", t =>
        {
            t.HasCheckConstraint("CK_StrippingRecipeLines_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_StrippingRecipeLines_Unit", UpperSnakeEnumConverter.CheckConstraintSql<RecipeAmountUnit>("Unit"));
        });
        builder.Property(l => l.Amount).HasPrecision(10, 4);

        builder.HasOne(l => l.Recipe).WithMany(r => r.Lines).HasForeignKey(l => l.RecipeId);
        builder.HasOne(l => l.DyeChemical).WithMany().HasForeignKey(l => l.DyeChemicalId);

        builder.HasIndex(l => new { l.RecipeId, l.Sequence }).IsUnique();
    }
}
