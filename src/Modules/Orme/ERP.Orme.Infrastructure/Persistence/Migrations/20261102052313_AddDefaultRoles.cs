using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Orme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Varsayılan Örme rolleri (kullanıcı onayı 2026-11-02); kurulumdan sonra yetkiler rol ekranından yönetilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Roles] ON;
EXEC(N'INSERT INTO [Core].[Roles] ([Id], [Code], [Name], [IsSystem], [ModuleKey])
SELECT v.[Id], v.[Code], v.[Name], v.[IsSystem], v.[ModuleKey] FROM (VALUES
(301, N''ORME_MANAGER'', N''Örme Yöneticisi'', 1, N''Orme''),
(302, N''ORME_SALES'', N''Örme Satış ve Planlama'', 1, N''Orme''),
(303, N''ORME_PRODUCTION'', N''Örme Üretim Sorumlusu'', 1, N''Orme''),
(304, N''ORME_QUALITY'', N''Örme Kalite Kontrol'', 1, N''Orme''),
(305, N''ORME_QUALITY_LEAD'', N''Örme Kalite Sorumlusu'', 1, N''Orme''),
(306, N''ORME_WAREHOUSE'', N''Örme Depo ve Sevkiyat'', 1, N''Orme''),
(307, N''ORME_ACCOUNTING'', N''Örme Muhasebe'', 1, N''Orme'')
) v([Id], [Code], [Name], [IsSystem], [ModuleKey])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[Id] OR r.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Roles] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id] FROM (VALUES
(N'ORME_MANAGER', N'Orme.View'),
(N'ORME_MANAGER', N'Orme.Customer.View'),
(N'ORME_MANAGER', N'Orme.Customer.Manage'),
(N'ORME_MANAGER', N'Orme.Material.View'),
(N'ORME_MANAGER', N'Orme.Material.Manage'),
(N'ORME_MANAGER', N'Orme.Machine.View'),
(N'ORME_MANAGER', N'Orme.Machine.Manage'),
(N'ORME_MANAGER', N'Orme.Order.View'),
(N'ORME_MANAGER', N'Orme.Order.Manage'),
(N'ORME_MANAGER', N'Orme.Receipt.View'),
(N'ORME_MANAGER', N'Orme.Receipt.Manage'),
(N'ORME_MANAGER', N'Orme.Knitting.View'),
(N'ORME_MANAGER', N'Orme.Knitting.Manage'),
(N'ORME_MANAGER', N'Orme.Inspection.View'),
(N'ORME_MANAGER', N'Orme.Inspection.Manage'),
(N'ORME_MANAGER', N'Orme.Inspection.Override'),
(N'ORME_MANAGER', N'Orme.Packing.View'),
(N'ORME_MANAGER', N'Orme.Packing.Manage'),
(N'ORME_MANAGER', N'Orme.Shipment.View'),
(N'ORME_MANAGER', N'Orme.Shipment.Manage'),
(N'ORME_MANAGER', N'Orme.Invoice.View'),
(N'ORME_MANAGER', N'Orme.Invoice.Manage'),
(N'ORME_MANAGER', N'Orme.Account.View'),
(N'ORME_MANAGER', N'Orme.Account.Manage'),
(N'ORME_MANAGER', N'Orme.Floor.View'),
(N'ORME_MANAGER', N'Orme.Floor.Manage'),
(N'ORME_MANAGER', N'Orme.Report.View'),
(N'ORME_MANAGER', N'Orme.Purchase.View'),
(N'ORME_MANAGER', N'Orme.Purchase.Manage'),
(N'ORME_SALES', N'Orme.View'),
(N'ORME_SALES', N'Orme.Customer.View'),
(N'ORME_SALES', N'Orme.Customer.Manage'),
(N'ORME_SALES', N'Orme.Order.View'),
(N'ORME_SALES', N'Orme.Order.Manage'),
(N'ORME_SALES', N'Orme.Report.View'),
(N'ORME_SALES', N'Orme.Material.View'),
(N'ORME_SALES', N'Orme.Machine.View'),
(N'ORME_SALES', N'Orme.Knitting.View'),
(N'ORME_PRODUCTION', N'Orme.View'),
(N'ORME_PRODUCTION', N'Orme.Knitting.View'),
(N'ORME_PRODUCTION', N'Orme.Knitting.Manage'),
(N'ORME_PRODUCTION', N'Orme.Floor.View'),
(N'ORME_PRODUCTION', N'Orme.Floor.Manage'),
(N'ORME_PRODUCTION', N'Orme.Report.View'),
(N'ORME_PRODUCTION', N'Orme.Order.View'),
(N'ORME_PRODUCTION', N'Orme.Receipt.View'),
(N'ORME_QUALITY', N'Orme.View'),
(N'ORME_QUALITY', N'Orme.Inspection.View'),
(N'ORME_QUALITY', N'Orme.Inspection.Manage'),
(N'ORME_QUALITY', N'Orme.Report.View'),
(N'ORME_QUALITY', N'Orme.Knitting.View'),
(N'ORME_QUALITY_LEAD', N'Orme.View'),
(N'ORME_QUALITY_LEAD', N'Orme.Inspection.View'),
(N'ORME_QUALITY_LEAD', N'Orme.Inspection.Manage'),
(N'ORME_QUALITY_LEAD', N'Orme.Inspection.Override'),
(N'ORME_QUALITY_LEAD', N'Orme.Report.View'),
(N'ORME_QUALITY_LEAD', N'Orme.Knitting.View'),
(N'ORME_WAREHOUSE', N'Orme.View'),
(N'ORME_WAREHOUSE', N'Orme.Receipt.View'),
(N'ORME_WAREHOUSE', N'Orme.Receipt.Manage'),
(N'ORME_WAREHOUSE', N'Orme.Packing.View'),
(N'ORME_WAREHOUSE', N'Orme.Packing.Manage'),
(N'ORME_WAREHOUSE', N'Orme.Shipment.View'),
(N'ORME_WAREHOUSE', N'Orme.Shipment.Manage'),
(N'ORME_ACCOUNTING', N'Orme.View'),
(N'ORME_ACCOUNTING', N'Orme.Invoice.View'),
(N'ORME_ACCOUNTING', N'Orme.Invoice.Manage'),
(N'ORME_ACCOUNTING', N'Orme.Account.View'),
(N'ORME_ACCOUNTING', N'Orme.Account.Manage'),
(N'ORME_ACCOUNTING', N'Orme.Order.View'),
(N'ORME_ACCOUNTING', N'Orme.Shipment.View')
) v([RoleCode], [PermissionCode])
JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
JOIN [Core].[Permissions] p ON p.[Code] = v.[PermissionCode]
WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE rp FROM [Core].[RolePermissions] rp JOIN [Core].[Roles] r ON r.[Id] = rp.[RoleId] WHERE r.[Code] IN ('ORME_MANAGER', 'ORME_SALES', 'ORME_PRODUCTION', 'ORME_QUALITY', 'ORME_QUALITY_LEAD', 'ORME_WAREHOUSE', 'ORME_ACCOUNTING');
DELETE FROM [Core].[Roles] WHERE [Code] IN ('ORME_MANAGER', 'ORME_SALES', 'ORME_PRODUCTION', 'ORME_QUALITY', 'ORME_QUALITY_LEAD', 'ORME_WAREHOUSE', 'ORME_ACCOUNTING');");

        }
    }
}
