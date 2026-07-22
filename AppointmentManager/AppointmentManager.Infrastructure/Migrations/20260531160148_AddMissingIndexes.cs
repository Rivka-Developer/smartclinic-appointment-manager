using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkShifts_Date",
                table: "WorkShifts",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status",
                table: "Appointments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkShifts_Date",
                table: "WorkShifts");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_Status",
                table: "Appointments");
        }
    }
}
