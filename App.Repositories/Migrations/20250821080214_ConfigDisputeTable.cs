using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ConfigDisputeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_booked_slots_booking_disputes_dispute_id",
                table: "booked_slots");

            migrationBuilder.DropForeignKey(
                name: "FK_booking_disputes_bookings_booking_id",
                table: "booking_disputes");

            migrationBuilder.DropForeignKey(
                name: "FK_bookings_booking_disputes_current_dispute_id",
                table: "bookings");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_current_dispute_id",
                table: "bookings",
                newName: "ix_bookings_current_dispute_id");

            migrationBuilder.RenameIndex(
                name: "IX_booking_disputes_booking_id",
                table: "booking_disputes",
                newName: "ix_booking_disputes_booking_id");

            migrationBuilder.RenameIndex(
                name: "ix_booked_slots_dispute_id",
                table: "booked_slots",
                newName: "IX_booked_slots_dispute_id");

            migrationBuilder.AlterColumn<string>(
                name: "booking_id",
                table: "booking_disputes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "booked_slot_id",
                table: "booking_disputes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_disputes_booked_slot_id",
                table: "booking_disputes",
                column: "booked_slot_id");

            migrationBuilder.AddForeignKey(
                name: "FK_booked_slots_booking_disputes_dispute_id",
                table: "booked_slots",
                column: "dispute_id",
                principalTable: "booking_disputes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_booked_slots_booking_disputes_dispute_id1",
                table: "booking_disputes",
                column: "booked_slot_id",
                principalTable: "booked_slots",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_booked_slots_booking_disputes_dispute_id",
                table: "booked_slots");

            migrationBuilder.DropForeignKey(
                name: "fk_booked_slots_booking_disputes_dispute_id1",
                table: "booking_disputes");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_disputes_bookings_booking_id",
                table: "booking_disputes");

            migrationBuilder.DropForeignKey(
                name: "fk_bookings__booking_disputes_current_dispute_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_booking_disputes_booked_slot_id",
                table: "booking_disputes");

            migrationBuilder.DropColumn(
                name: "booked_slot_id",
                table: "booking_disputes");

            migrationBuilder.RenameIndex(
                name: "ix_bookings_current_dispute_id",
                table: "bookings",
                newName: "IX_bookings_current_dispute_id");

            migrationBuilder.RenameIndex(
                name: "ix_booking_disputes_booking_id",
                table: "booking_disputes",
                newName: "IX_booking_disputes_booking_id");

            migrationBuilder.RenameIndex(
                name: "IX_booked_slots_dispute_id",
                table: "booked_slots",
                newName: "ix_booked_slots_dispute_id");

            migrationBuilder.AlterColumn<string>(
                name: "booking_id",
                table: "booking_disputes",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_booked_slots_booking_disputes_dispute_id",
                table: "booked_slots",
                column: "dispute_id",
                principalTable: "booking_disputes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_booking_disputes_bookings_booking_id",
                table: "booking_disputes",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_booking_disputes_current_dispute_id",
                table: "bookings",
                column: "current_dispute_id",
                principalTable: "booking_disputes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
