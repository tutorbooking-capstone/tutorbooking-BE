using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ConfigDispute2BookingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_booking_disputes_bookings_booking_id",
                table: "booking_disputes");

            migrationBuilder.DropForeignKey(
                name: "fk_bookings__booking_disputes_current_dispute_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_bookings_current_dispute_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_booking_disputes_booking_id",
                table: "booking_disputes");

            migrationBuilder.DropColumn(
                name: "current_dispute_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "booking_id",
                table: "booking_disputes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "current_dispute_id",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "booking_id",
                table: "booking_disputes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_current_dispute_id",
                table: "bookings",
                column: "current_dispute_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_booking_disputes_booking_id",
                table: "booking_disputes",
                column: "booking_id");

            migrationBuilder.AddForeignKey(
                name: "fk_booking_disputes_bookings_booking_id",
                table: "booking_disputes",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_bookings__booking_disputes_current_dispute_id",
                table: "bookings",
                column: "current_dispute_id",
                principalTable: "booking_disputes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
