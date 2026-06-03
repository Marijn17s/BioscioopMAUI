using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioscoopMAUI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalPriceToReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "Reservations",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "Reservations");
        }
    }
}
