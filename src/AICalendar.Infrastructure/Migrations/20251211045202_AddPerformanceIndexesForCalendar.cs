using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICalendar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexesForCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Predictions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 11, 4, 52, 0, 843, DateTimeKind.Utc).AddTicks(9060));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 11, 4, 52, 0, 843, DateTimeKind.Utc).AddTicks(9060));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 11, 4, 52, 0, 843, DateTimeKind.Utc).AddTicks(9060));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 11, 4, 52, 0, 843, DateTimeKind.Utc).AddTicks(9060));

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_CreatedAt",
                table: "Predictions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_Status",
                table: "Predictions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_UserId",
                table: "Predictions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionItems_DueDate",
                table: "PredictionItems",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionItems_IsAccepted",
                table: "PredictionItems",
                column: "IsAccepted");

            migrationBuilder.CreateIndex(
                name: "IX_Calendars_CreatedAt",
                table: "Calendars",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Calendars_UpdatedAt",
                table: "Calendars",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarItems_IsPaid",
                table: "CalendarItems",
                column: "IsPaid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Predictions_CreatedAt",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_Status",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_UserId",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_PredictionItems_DueDate",
                table: "PredictionItems");

            migrationBuilder.DropIndex(
                name: "IX_PredictionItems_IsAccepted",
                table: "PredictionItems");

            migrationBuilder.DropIndex(
                name: "IX_Calendars_CreatedAt",
                table: "Calendars");

            migrationBuilder.DropIndex(
                name: "IX_Calendars_UpdatedAt",
                table: "Calendars");

            migrationBuilder.DropIndex(
                name: "IX_CalendarItems_IsPaid",
                table: "CalendarItems");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Predictions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 10, 5, 27, 30, 970, DateTimeKind.Utc).AddTicks(7794));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 10, 5, 27, 30, 970, DateTimeKind.Utc).AddTicks(7794));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 10, 5, 27, 30, 970, DateTimeKind.Utc).AddTicks(7794));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 10, 5, 27, 30, 970, DateTimeKind.Utc).AddTicks(7794));
        }
    }
}
