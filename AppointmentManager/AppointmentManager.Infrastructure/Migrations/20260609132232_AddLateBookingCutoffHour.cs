using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLateBookingCutoffHour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LateBookingCutoffHour",
                table: "Settings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LateBookingCutoffHour",
                table: "Settings");
        }
    }
}
