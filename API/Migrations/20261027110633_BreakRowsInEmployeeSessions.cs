using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class BreakRowsInEmployeeSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionBreaks");

            migrationBuilder.AddColumn<DateTime>(
                name: "BreakEndedAt",
                table: "EmployeeSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BreakStartedAt",
                table: "EmployeeSessions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BreakEndedAt",
                table: "EmployeeSessions");

            migrationBuilder.DropColumn(
                name: "BreakStartedAt",
                table: "EmployeeSessions");

            migrationBuilder.CreateTable(
                name: "SessionBreaks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionBreaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionBreaks_EmployeeSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "EmployeeSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionBreaks_SessionId",
                table: "SessionBreaks",
                column: "SessionId");
        }
    }
}
