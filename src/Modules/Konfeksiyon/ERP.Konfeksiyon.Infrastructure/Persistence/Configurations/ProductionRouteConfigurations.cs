using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Configurations;

public sealed class ProductionRouteConfiguration : IEntityTypeConfiguration<ProductionRoute>
{
    public void Configure(EntityTypeBuilder<ProductionRoute> builder)
    {
        builder.ToTable("ProductionRoutes", t =>
        {
            t.HasCheckConstraint(
                "CK_ProductionRoutes_Status", UpperSnakeEnumConverter.CheckConstraintSql<RouteStatus>("Status"));
            t.HasCheckConstraint("CK_ProductionRoutes_Version", "[Version] > 0");
        });

        builder.Property(r => r.Name).HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasOne(r => r.Model).WithMany().HasForeignKey(r => r.ModelId);

        // Sürüm numarası silinen rotalarda da yeniden kullanılmaz.
        builder.HasIndex(r => new { r.ModelId, r.Version }).IsUnique();

        // Model başına en fazla bir etkin rota: eşzamanlı iki etkinleştirmeden yalnızca biri başarılı olur.
        builder.HasIndex(r => r.ModelId)
            .IsUnique()
            .HasDatabaseName("UX_ProductionRoutes_ActivePerModel")
            .HasFilter("[Status] = 'ACTIVE' AND [IsDeleted] = 0");
    }
}

public sealed class ProductionRouteOperationConfiguration : IEntityTypeConfiguration<ProductionRouteOperation>
{
    public void Configure(EntityTypeBuilder<ProductionRouteOperation> builder)
    {
        builder.ToTable("ProductionRouteOperations", t =>
        {
            t.HasCheckConstraint("CK_ProductionRouteOperations_SequenceNo", "[SequenceNo] > 0");
            t.HasCheckConstraint("CK_ProductionRouteOperations_Sam", "[Sam] >= 0");
        });

        builder.Property(o => o.Sam).HasPrecision(9, 4);

        builder.HasOne(o => o.ProductionRoute).WithMany(r => r.Operations).HasForeignKey(o => o.ProductionRouteId);
        builder.HasOne(o => o.Operation).WithMany().HasForeignKey(o => o.OperationId);
        builder.HasOne(o => o.Subcontractor).WithMany().HasForeignKey(o => o.SubcontractorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.ProductionRouteId, o.SequenceNo }).IsUnique();
        builder.HasIndex(o => o.OperationId);
        builder.HasIndex(o => o.SubcontractorId);

        builder.HasQueryFilter(o => !o.ProductionRoute.IsDeleted);
    }
}

public sealed class OperationDependencyConfiguration : IEntityTypeConfiguration<OperationDependency>
{
    public void Configure(EntityTypeBuilder<OperationDependency> builder)
    {
        builder.ToTable(
            "OperationDependencies",
            t => t.HasCheckConstraint("CK_OperationDependencies_NotSelf", "[RouteOperationId] <> [PredecessorId]"));

        builder.HasKey(d => new { d.RouteOperationId, d.PredecessorId });

        builder.HasOne(d => d.RouteOperation).WithMany(o => o.Predecessors).HasForeignKey(d => d.RouteOperationId);
        builder.HasOne(d => d.Predecessor).WithMany(o => o.Successors).HasForeignKey(d => d.PredecessorId);
        builder.HasIndex(d => d.PredecessorId);

        builder.HasQueryFilter(d => !d.RouteOperation.ProductionRoute.IsDeleted);
    }
}
