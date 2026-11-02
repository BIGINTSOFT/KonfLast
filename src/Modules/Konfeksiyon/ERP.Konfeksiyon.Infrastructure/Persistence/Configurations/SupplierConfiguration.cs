using ERP.Core.Infrastructure.Persistence.Configurations;
using ERP.Konfeksiyon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.Property(s => s.SupplierCode).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.TaxNumber).HasMaxLength(20);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(256);
        builder.Property(s => s.Address).HasMaxLength(500);

        builder.HasIndex(s => s.SupplierCode).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
    }
}
