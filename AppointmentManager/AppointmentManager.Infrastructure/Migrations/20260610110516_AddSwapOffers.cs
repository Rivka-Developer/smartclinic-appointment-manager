using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSwapOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SwapOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferedByClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedByClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwapOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SwapOffers_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SwapOffers_Users_OfferedByClientId",
                        column: x => x.OfferedByClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SwapOffers_AcceptedByClientId",
                table: "SwapOffers",
                column: "AcceptedByClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SwapOffers_AppointmentId_Status",
                table: "SwapOffers",
                columns: new[] { "AppointmentId", "Status" },
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SwapOffers_OfferedByClientId",
                table: "SwapOffers",
                column: "OfferedByClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SwapOffers_Status",
                table: "SwapOffers",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SwapOffers");
        }
    }
}
