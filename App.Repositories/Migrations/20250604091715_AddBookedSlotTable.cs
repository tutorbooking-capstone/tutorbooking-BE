using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddBookedSlotTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_availability_slots__booking_slots_booking_slot_id",
                table: "availability_slots");

            migrationBuilder.DropIndex(
                name: "ix_availability_slots_booking_slot_id",
                table: "availability_slots");

            migrationBuilder.DropColumn(
                name: "repeat_for_weeks",
                table: "booking_slots");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "booking_slots");

            migrationBuilder.DropColumn(
                name: "booking_slot_id",
                table: "availability_slots");

            migrationBuilder.CreateTable(
                name: "booked_slots",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    booking_slot_id = table.Column<string>(type: "text", nullable: false),
                    availability_slot_id = table.Column<string>(type: "text", nullable: false),
                    booked_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    slot_note = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booked_slots", x => x.id);
                    table.ForeignKey(
                        name: "fk_booked_slots__booking_slots_booking_slot_id",
                        column: x => x.booking_slot_id,
                        principalTable: "booking_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booked_slots_availability_slots_availability_slot_id",
                        column: x => x.availability_slot_id,
                        principalTable: "availability_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_booked_slots_availability_slot_id",
                table: "booked_slots",
                column: "availability_slot_id");

            migrationBuilder.CreateIndex(
                name: "ix_booked_slots_booking_slot_id",
                table: "booked_slots",
                column: "booking_slot_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booked_slots");

            migrationBuilder.AddColumn<int>(
                name: "repeat_for_weeks",
                table: "booking_slots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "start_date",
                table: "booking_slots",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "booking_slot_id",
                table: "availability_slots",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_availability_slots_booking_slot_id",
                table: "availability_slots",
                column: "booking_slot_id");

            migrationBuilder.AddForeignKey(
                name: "fk_availability_slots__booking_slots_booking_slot_id",
                table: "availability_slots",
                column: "booking_slot_id",
                principalTable: "booking_slots",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
