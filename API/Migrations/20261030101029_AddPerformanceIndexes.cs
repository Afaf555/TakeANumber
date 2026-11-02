using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_CounterId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ServiceId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeSessions_UserId",
                table: "EmployeeSessions");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CounterId_StartedAt",
                table: "Tickets",
                columns: new[] { "CounterId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ServiceId_CreatedAt",
                table: "Tickets",
                columns: new[] { "ServiceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSessions_CounterId_LoginAt",
                table: "EmployeeSessions",
                columns: new[] { "CounterId", "LoginAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSessions_UserId_LoginAt",
                table: "EmployeeSessions",
                columns: new[] { "UserId", "LoginAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_ActorId_CreatedAt",
                table: "ActivityLogs",
                columns: new[] { "ActorId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_CounterId_StartedAt",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ServiceId_CreatedAt",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeSessions_CounterId_LoginAt",
                table: "EmployeeSessions");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeSessions_UserId_LoginAt",
                table: "EmployeeSessions");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_ActorId_CreatedAt",
                table: "ActivityLogs");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CounterId",
                table: "Tickets",
                column: "CounterId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ServiceId",
                table: "Tickets",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSessions_UserId",
                table: "EmployeeSessions",
                column: "UserId");
        }
    }
}
