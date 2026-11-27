using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class TicketEmployeeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "Tickets",
                type: "int",
                nullable: true);
            migrationBuilder.Sql("""
    UPDATE t
    SET EmployeeId = (
        SELECT TOP 1 s.UserId
        FROM EmployeeSessions s
        WHERE s.BreakStartedAt IS NULL
          AND s.CounterId = t.CounterId
          AND s.LoginAt <= t.StartedAt
          AND t.StartedAt <= ISNULL(s.LogoutAt, DATEADD(day, 1, s.LoginAt))
        ORDER BY s.LoginAt DESC)
    FROM Tickets t
    WHERE t.StartedAt IS NOT NULL AND t.CounterId IS NOT NULL AND t.EmployeeId IS NULL;
    """);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_EmployeeId_StartedAt",
                table: "Tickets",
                columns: new[] { "EmployeeId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_EmployeeId_StartedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Tickets");
        }
    }
}
