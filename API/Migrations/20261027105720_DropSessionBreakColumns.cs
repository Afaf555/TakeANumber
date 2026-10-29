using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class DropSessionBreakColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BreakEndedAt",
                table: "EmployeeSessions");

            migrationBuilder.DropColumn(
                name: "BreakMinutes",
                table: "EmployeeSessions");

            migrationBuilder.DropColumn(
                name: "BreakStartedAt",
                table: "EmployeeSessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BreakEndedAt",
                table: "EmployeeSessions",
                type: "datetime2",
                nullable: true);

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
