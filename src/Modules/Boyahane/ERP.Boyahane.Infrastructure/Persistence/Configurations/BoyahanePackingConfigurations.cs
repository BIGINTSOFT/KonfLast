using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Boyahane.Infrastructure.Persistence.Configurations;

public sealed class BoyahanePackingListConfiguration : IEntityTypeConfiguration<BoyahanePackingList>
{
    public void Configure(EntityTypeBuilder<BoyahanePackingList> builder)
    {
        builder.ToTable("PackingLists", t => t.HasCheckConstraint("CK_PackingLists_Status", UpperSnakeEnumConverter.CheckConstraintSql<PackingListStatus>("Status")));
        builder.Property(l => l.PackingNo).HasMaxLength(30).IsRequired();
        builder.Property(l => l.Notes).HasMaxLength(1000);
        builder.HasOne(l => l.Order).WithMany().HasForeignKey(l => l.OrderId);
        builder.HasIndex(l => l.PackingNo).IsUnique();
    }
}

public sealed class BoyahanePackingItemConfiguration : IEntityTypeConfiguration<BoyahanePackingItem>
{
    public void Configure(EntityTypeBuilder<BoyahanePackingItem> builder)
    {
        builder.ToTable("PackingItems", t =>
        {
            t.HasCheckConstraint("CK_PackingItems_Kilograms", "[Kilograms] > 0");
            t.HasCheckConstraint("CK_PackingItems_BoxCount", "[BoxCount] >= 1");
        });

        builder.Property(i => i.PackageNo).HasMaxLength(40).IsRequired();
        builder.Property(i => i.Kilograms).HasPrecision(12, 2);
        builder.Property(i => i.Meters).HasPrecision(12, 2);

        builder.HasOne(i => i.PackingList).WithMany(l => l.Items).HasForeignKey(i => i.PackingListId);
        builder.HasOne(i => i.BatchItem).WithMany().HasForeignKey(i => i.BatchItemId);

        builder.HasIndex(i => i.PackageNo).IsUnique();

        // Bir mal aynı anda tek listede olur.
        builder.HasIndex(i => i.BatchItemId, "UX_PackingItems_Active").IsUnique().HasFilter("[IsActive] = 1");
    }
}
