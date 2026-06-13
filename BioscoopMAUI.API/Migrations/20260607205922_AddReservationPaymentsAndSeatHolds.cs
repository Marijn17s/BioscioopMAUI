using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioscoopMAUI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationPaymentsAndSeatHolds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HoldAuth0UserId",
                table: "ShowtimeSeats",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "HoldExpiresAtUtc",
                table: "ShowtimeSeats",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HoldId",
                table: "ShowtimeSeats",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalPrice",
                table: "Reservations",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Reservations",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Reservations",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Confirmed")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "Reservations",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StripeSessionId",
                table: "Reservations",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ShowtimeSeats_HoldId",
                table: "ShowtimeSeats",
                column: "HoldId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_StripeSessionId",
                table: "Reservations",
                column: "StripeSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShowtimeSeats_HoldId",
                table: "ShowtimeSeats");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_StripeSessionId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "HoldAuth0UserId",
                table: "ShowtimeSeats");

            migrationBuilder.DropColumn(
                name: "HoldExpiresAtUtc",
                table: "ShowtimeSeats");

            migrationBuilder.DropColumn(
                name: "HoldId",
                table: "ShowtimeSeats");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "StripeSessionId",
                table: "Reservations");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalPrice",
                table: "Reservations",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);
        }
    }
}
