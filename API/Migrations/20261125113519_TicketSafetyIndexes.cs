using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class TicketSafetyIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Tickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "TicketDate",
                table: "Tickets",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
            // 1) fill TicketDate for existing tickets (local day)
            migrationBuilder.Sql("""
    UPDATE Tickets
    SET TicketDate = CAST((CreatedAt AT TIME ZONE 'UTC') AT TIME ZONE 'Central European Standard Time' AS date);
    """);

            // 2) duplicate numbers from before the fix: first one keeps its number,
            //    the others get new numbers after the last number of that day
            migrationBuilder.Sql("""
    WITH d AS (
        SELECT Id, TicketDate,
               ROW_NUMBER() OVER (PARTITION BY TicketDate, Number ORDER BY Id) AS rn
        FROM Tickets),
    extra AS (
        SELECT Id, TicketDate,
               ROW_NUMBER() OVER (PARTITION BY TicketDate ORDER BY Id) AS k
        FROM d WHERE rn > 1)
    UPDATE t
    SET Number = m.MaxNumber + e.k
    FROM Tickets t
    JOIN extra e ON e.Id = t.Id
    JOIN (SELECT TicketDate, MAX(Number) AS MaxNumber FROM Tickets GROUP BY TicketDate) m
      ON m.TicketDate = e.TicketDate;

    UPDATE c
    SET LastNumber = m.MaxNumber
    FROM TicketCounters c
    JOIN (SELECT TicketDate, MAX(Number) AS MaxNumber FROM Tickets GROUP BY TicketDate) m
      ON m.TicketDate = c.[Date]
    WHERE m.MaxNumber > c.LastNumber;
    """);

            // 3) a counter with more than one InProgress ticket on the same day:
            //    keep the newest, close the older ones
            migrationBuilder.Sql("""
    WITH p AS (
        SELECT Id,
               ROW_NUMBER() OVER (PARTITION BY CounterId, TicketDate ORDER BY StartedAt DESC, Id DESC) AS rn
        FROM Tickets
        WHERE Status = 'InProgress' AND CounterId IS NOT NULL)
    UPDATE Tickets
    SET Status = 'Finished', FinishedAt = StartedAt
    WHERE Id IN (SELECT Id FROM p WHERE rn > 1);
    """);
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_OneInProgressPerCounter",
                table: "Tickets",
                columns: new[] { "CounterId", "TicketDate" },
                unique: true,
                filter: "[Status] = 'InProgress'");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TicketDate_Number",
                table: "Tickets",
                columns: new[] { "TicketDate", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_OneInProgressPerCounter",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_TicketDate_Number",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TicketDate",
                table: "Tickets");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
