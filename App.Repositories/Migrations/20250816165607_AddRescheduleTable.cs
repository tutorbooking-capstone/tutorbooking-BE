using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddRescheduleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "tutor_booking_offer_id",
                table: "offered_slots",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "is_for_reschedule",
                table: "offered_slots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "reschedule_request_id",
                table: "offered_slots",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "reschedule_requests",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    booked_slot_id = table.Column<string>(type: "text", nullable: false),
                    requested_by_user_id = table.Column<string>(type: "text", nullable: false),
                    initiator = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    response_note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    responded_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    accepted_slot_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reschedule_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_reschedule_requests_offered_slots_accepted_slot_id",
                        column: x => x.accepted_slot_id,
                        principalTable: "offered_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_reschedule_requests__booked_slots_booked_slot_id",
                        column: x => x.booked_slot_id,
                        principalTable: "booked_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_offered_slots_reschedule_request_id",
                table: "offered_slots",
                column: "reschedule_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_reschedule_requests_accepted_slot_id",
                table: "reschedule_requests",
                column: "accepted_slot_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reschedule_requests_booked_slot_id",
                table: "reschedule_requests",
                column: "booked_slot_id");

            migrationBuilder.AddForeignKey(
                name: "FK_offered_slots_reschedule_requests_reschedule_request_id",
                table: "offered_slots",
                column: "reschedule_request_id",
                principalTable: "reschedule_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_offered_slots_reschedule_requests_reschedule_request_id",
                table: "offered_slots");

            migrationBuilder.DropTable(
                name: "reschedule_requests");

            migrationBuilder.DropIndex(
                name: "IX_offered_slots_reschedule_request_id",
                table: "offered_slots");

            migrationBuilder.DropColumn(
                name: "is_for_reschedule",
                table: "offered_slots");

            migrationBuilder.DropColumn(
                name: "reschedule_request_id",
                table: "offered_slots");

            migrationBuilder.AlterColumn<string>(
                name: "tutor_booking_offer_id",
                table: "offered_slots",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
