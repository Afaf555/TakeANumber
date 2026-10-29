using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddNonWorkingDaysAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_NonWorkingDay",
                table: "NonWorkingDay");

            migrationBuilder.RenameTable(
                name: "NonWorkingDay",
                newName: "NonWorkingDays");

            migrationBuilder.RenameIndex(
                name: "IX_NonWorkingDay_Date",
                table: "NonWorkingDays",
                newName: "IX_NonWorkingDays_Date");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NonWorkingDays",
                table: "NonWorkingDays",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_NonWorkingDays",
                table: "NonWorkingDays");

            migrationBuilder.RenameTable(
                name: "NonWorkingDays",
                newName: "NonWorkingDay");

            migrationBuilder.RenameIndex(
                name: "IX_NonWorkingDays_Date",
                table: "NonWorkingDay",
                newName: "IX_NonWorkingDay_Date");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NonWorkingDay",
                table: "NonWorkingDay",
                column: "Id");
        }
    }
}
