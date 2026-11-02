using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultBreakdownReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"INSERT INTO [Boyahane].[StopReasons] ([Code], [Name], [Category], [SortOrder], [IsActive], [CreatedAt], [IsDeleted])
SELECT v.[Code], v.[Name], 'BREAKDOWN', v.[SortOrder], 1, SYSUTCDATETIME(), 0 FROM (VALUES
(N'POMPA', N'Pompa arızası', 10),
(N'ISITMA', N'Isıtma / buhar arızası', 20),
(N'MOTOR', N'Motor arızası', 30),
(N'ELEKTRIK', N'Elektrik arızası', 40),
(N'OTOMASYON', N'Otomasyon / kontrol arızası', 50),
(N'SU-TESISAT', N'Su / tesisat sorunu', 60),
(N'ARIZA-DIGER', N'Diğer arıza', 70)
) v([Code], [Name], [SortOrder])
WHERE NOT EXISTS (SELECT 1 FROM [Boyahane].[StopReasons] r WHERE r.[Code] = v.[Code])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Boyahane].[StopReasons] WHERE [Category] = 'BREAKDOWN'
AND [Code] IN (N'POMPA', N'ISITMA', N'MOTOR', N'ELEKTRIK', N'OTOMASYON', N'SU-TESISAT', N'ARIZA-DIGER')");
        }
    }
}
