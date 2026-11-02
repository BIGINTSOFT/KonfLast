using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Dokuma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Varsayılan Dokuma rolleri (kullanıcı onayı 2026-11-01); kurulumdan sonra yetkiler rol ekranından yönetilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Roles] ON;
EXEC(N'INSERT INTO [Core].[Roles] ([Id], [Code], [Name], [IsSystem], [ModuleKey])
SELECT v.[Id], v.[Code], v.[Name], v.[IsSystem], v.[ModuleKey] FROM (VALUES
(201, N''DOKUMA_MANAGER'', N''Dokuma Yöneticisi'', 1, N''Dokuma''),
(202, N''DOKUMA_SALES'', N''Dokuma Satış ve Planlama'', 1, N''Dokuma''),
(203, N''DOKUMA_PRODUCTION'', N''Dokuma Üretim Sorumlusu'', 1, N''Dokuma''),
(204, N''DOKUMA_QUALITY'', N''Dokuma Kalite Kontrol'', 1, N''Dokuma''),
(205, N''DOKUMA_QUALITY_LEAD'', N''Dokuma Kalite Sorumlusu'', 1, N''Dokuma''),
(206, N''DOKUMA_WAREHOUSE'', N''Dokuma Depo ve Sevkiyat'', 1, N''Dokuma''),
(207, N''DOKUMA_ACCOUNTING'', N''Dokuma Muhasebe'', 1, N''Dokuma'')
) v([Id], [Code], [Name], [IsSystem], [ModuleKey])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[Id] OR r.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Roles] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id] FROM (VALUES
(N'DOKUMA_MANAGER', N'Dokuma.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Customer.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Customer.Manage'),
(N'DOKUMA_MANAGER', N'Dokuma.Material.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Material.Manage'),
(N'DOKUMA_MANAGER', N'Dokuma.Loom.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Loom.Manage'),
(N'DOKUMA_MANAGER', N'Dokuma.Purchase.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Purchase.Manage'),
(N'DOKUMA_MANAGER', N'Dokuma.Order.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Order.Manage'),
(N'DOKUMA_MANAGER', N'Dokuma.Receipt.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Receipt.Manage'),
(N'DOKUMA_MANAGER', N'Dokuma.Warp.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Warp.Manage'),
(N'DOKUMA_MANAGER', N'Dokuma.Weaving.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Weaving.Manage'),
(N'DOKUMA_MANAGER', N'Dokuma.Inspection.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Inspection.Manage'),
(N'DOKUMA_MANAGER', N'Dokuma.Packing.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Packing.Manage'),
(N'DOKUMA_MANAGER', N'Dokuma.Shipment.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Shipment.Manage'),
(N'DOKUMA_MANAGER', N'Dokuma.Invoice.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Invoice.Manage'),
(N'DOKUMA_MANAGER', N'Dokuma.Account.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Account.Manage'),
(N'DOKUMA_MANAGER', N'Dokuma.Floor.View'),
(N'DOKUMA_MANAGER', N'Dokuma.Floor.Manage'),
(N'DOKUMA_MANAGER', N'Dokuma.Inspection.Override'),
(N'DOKUMA_MANAGER', N'Dokuma.Report.View'),
(N'DOKUMA_SALES', N'Dokuma.View'),
(N'DOKUMA_SALES', N'Dokuma.Customer.View'),
(N'DOKUMA_SALES', N'Dokuma.Customer.Manage'),
(N'DOKUMA_SALES', N'Dokuma.Order.View'),
(N'DOKUMA_SALES', N'Dokuma.Order.Manage'),
(N'DOKUMA_SALES', N'Dokuma.Report.View'),
(N'DOKUMA_SALES', N'Dokuma.Material.View'),
(N'DOKUMA_SALES', N'Dokuma.Loom.View'),
(N'DOKUMA_SALES', N'Dokuma.Warp.View'),
(N'DOKUMA_SALES', N'Dokuma.Weaving.View'),
(N'DOKUMA_PRODUCTION', N'Dokuma.View'),
(N'DOKUMA_PRODUCTION', N'Dokuma.Warp.View'),
(N'DOKUMA_PRODUCTION', N'Dokuma.Warp.Manage'),
(N'DOKUMA_PRODUCTION', N'Dokuma.Weaving.View'),
(N'DOKUMA_PRODUCTION', N'Dokuma.Weaving.Manage'),
(N'DOKUMA_PRODUCTION', N'Dokuma.Floor.View'),
(N'DOKUMA_PRODUCTION', N'Dokuma.Floor.Manage'),
(N'DOKUMA_PRODUCTION', N'Dokuma.Report.View'),
(N'DOKUMA_PRODUCTION', N'Dokuma.Order.View'),
(N'DOKUMA_PRODUCTION', N'Dokuma.Receipt.View'),
(N'DOKUMA_QUALITY', N'Dokuma.View'),
(N'DOKUMA_QUALITY', N'Dokuma.Inspection.View'),
(N'DOKUMA_QUALITY', N'Dokuma.Inspection.Manage'),
(N'DOKUMA_QUALITY', N'Dokuma.Report.View'),
(N'DOKUMA_QUALITY', N'Dokuma.Warp.View'),
(N'DOKUMA_QUALITY', N'Dokuma.Weaving.View'),
(N'DOKUMA_QUALITY_LEAD', N'Dokuma.View'),
(N'DOKUMA_QUALITY_LEAD', N'Dokuma.Inspection.View'),
(N'DOKUMA_QUALITY_LEAD', N'Dokuma.Inspection.Manage'),
(N'DOKUMA_QUALITY_LEAD', N'Dokuma.Inspection.Override'),
(N'DOKUMA_QUALITY_LEAD', N'Dokuma.Report.View'),
(N'DOKUMA_QUALITY_LEAD', N'Dokuma.Warp.View'),
(N'DOKUMA_QUALITY_LEAD', N'Dokuma.Weaving.View'),
(N'DOKUMA_WAREHOUSE', N'Dokuma.View'),
(N'DOKUMA_WAREHOUSE', N'Dokuma.Receipt.View'),
(N'DOKUMA_WAREHOUSE', N'Dokuma.Receipt.Manage'),
(N'DOKUMA_WAREHOUSE', N'Dokuma.Packing.View'),
(N'DOKUMA_WAREHOUSE', N'Dokuma.Packing.Manage'),
(N'DOKUMA_WAREHOUSE', N'Dokuma.Shipment.View'),
(N'DOKUMA_WAREHOUSE', N'Dokuma.Shipment.Manage'),
(N'DOKUMA_ACCOUNTING', N'Dokuma.View'),
(N'DOKUMA_ACCOUNTING', N'Dokuma.Invoice.View'),
(N'DOKUMA_ACCOUNTING', N'Dokuma.Invoice.Manage'),
(N'DOKUMA_ACCOUNTING', N'Dokuma.Account.View'),
(N'DOKUMA_ACCOUNTING', N'Dokuma.Account.Manage'),
(N'DOKUMA_ACCOUNTING', N'Dokuma.Order.View'),
(N'DOKUMA_ACCOUNTING', N'Dokuma.Shipment.View')
) v([RoleCode], [PermissionCode])
JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
JOIN [Core].[Permissions] p ON p.[Code] = v.[PermissionCode]
WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE rp FROM [Core].[RolePermissions] rp JOIN [Core].[Roles] r ON r.[Id] = rp.[RoleId] WHERE r.[Code] IN ('DOKUMA_MANAGER', 'DOKUMA_SALES', 'DOKUMA_PRODUCTION', 'DOKUMA_QUALITY', 'DOKUMA_QUALITY_LEAD', 'DOKUMA_WAREHOUSE', 'DOKUMA_ACCOUNTING');
DELETE FROM [Core].[Roles] WHERE [Code] IN ('DOKUMA_MANAGER', 'DOKUMA_SALES', 'DOKUMA_PRODUCTION', 'DOKUMA_QUALITY', 'DOKUMA_QUALITY_LEAD', 'DOKUMA_WAREHOUSE', 'DOKUMA_ACCOUNTING');");

        }
    }
}
