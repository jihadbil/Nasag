using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nasag.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsLogoAndBackupKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "LogoBytes",
                table: "SchoolSettings",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "BackupLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoBytes",
                table: "SchoolSettings");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "BackupLogs");
        }
    }
}
