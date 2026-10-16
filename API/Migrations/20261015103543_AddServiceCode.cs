using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Services",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");
            migrationBuilder.Sql("UPDATE Services SET Code = 'S' + CAST(Id AS nvarchar(10))");
            migrationBuilder.CreateIndex(
                name: "IX_Services_Code",
                table: "Services",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Services_Code",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Services");
        }
    }
}
