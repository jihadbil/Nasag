using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nasag.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentPhotoBytes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PhotoBytes",
                table: "Students",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoBytes",
                table: "Students");
        }
    }
}
