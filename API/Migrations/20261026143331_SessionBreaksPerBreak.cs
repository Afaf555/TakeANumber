using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class SessionBreaksPerBreak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BreakMinutes",
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
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionBreaks");

            migrationBuilder.AddColumn<double>(
                name: "BreakMinutes",
                table: "EmployeeSessions",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "BreakStartedAt",
                table: "EmployeeSessions",
                type: "datetime2",
                nullable: true);
        }
    }
}
