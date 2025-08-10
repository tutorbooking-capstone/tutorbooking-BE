using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingFlowTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_booked_slots_availability_slots_availability_slot_id",
                table: "booked_slots");

            migrationBuilder.DropForeignKey(
                name: "fk_held_funds__booked_slots_booked_slot_id",
                table: "held_funds");

            migrationBuilder.DropIndex(
                name: "ix_held_funds_booked_slot_id",
                table: "held_funds");

            migrationBuilder.DropIndex(
                name: "ix_booked_slots_availability_slot_id",
                table: "booked_slots");

            migrationBuilder.DropColumn(
                name: "availability_slot_id",
                table: "booked_slots");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_time",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_time",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_updated_by",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_updated_time",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "lesson_snapshot_id",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_offer_id",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "booked_slots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_time",
                table: "booked_slots",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "booked_slots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_time",
                table: "booked_slots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "held_fund_id",
                table: "booked_slots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_updated_by",
                table: "booked_slots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_updated_time",
                table: "booked_slots",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "slot_index",
                table: "booked_slots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "lesson_snapshots",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    original_lesson_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    target_audience = table.Column<string>(type: "text", nullable: false),
                    prerequisites = table.Column<string>(type: "text", nullable: false),
                    language_code = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    duration_in_minutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lesson_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_lesson_snapshot_id",
                table: "bookings",
                column: "lesson_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "IX_booked_slots_held_fund_id",
                table: "booked_slots",
                column: "held_fund_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_booked_slots_held_funds_held_fund_id1",
                table: "booked_slots",
                column: "held_fund_id",
                principalTable: "held_funds",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_bookings__lesson_snapshots_lesson_snapshot_id",
                table: "bookings",
                column: "lesson_snapshot_id",
                principalTable: "lesson_snapshots",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_booked_slots_held_funds_held_fund_id1",
                table: "booked_slots");

            migrationBuilder.DropForeignKey(
                name: "fk_bookings__lesson_snapshots_lesson_snapshot_id",
                table: "bookings");

            migrationBuilder.DropTable(
                name: "lesson_snapshots");

            migrationBuilder.DropIndex(
                name: "ix_bookings_lesson_snapshot_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_booked_slots_held_fund_id",
                table: "booked_slots");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "created_time",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "deleted_time",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "last_updated_by",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "last_updated_time",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "lesson_snapshot_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "original_offer_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "booked_slots");

            migrationBuilder.DropColumn(
                name: "created_time",
                table: "booked_slots");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "booked_slots");

            migrationBuilder.DropColumn(
                name: "deleted_time",
                table: "booked_slots");

            migrationBuilder.DropColumn(
                name: "held_fund_id",
                table: "booked_slots");

            migrationBuilder.DropColumn(
                name: "last_updated_by",
                table: "booked_slots");

            migrationBuilder.DropColumn(
                name: "last_updated_time",
                table: "booked_slots");

            migrationBuilder.DropColumn(
                name: "slot_index",
                table: "booked_slots");

            migrationBuilder.AddColumn<string>(
                name: "availability_slot_id",
                table: "booked_slots",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_held_funds_booked_slot_id",
                table: "held_funds",
                column: "booked_slot_id");

            migrationBuilder.CreateIndex(
                name: "ix_booked_slots_availability_slot_id",
                table: "booked_slots",
                column: "availability_slot_id");

            migrationBuilder.AddForeignKey(
                name: "fk_booked_slots_availability_slots_availability_slot_id",
                table: "booked_slots",
                column: "availability_slot_id",
                principalTable: "availability_slots",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_held_funds__booked_slots_booked_slot_id",
                table: "held_funds",
                column: "booked_slot_id",
                principalTable: "booked_slots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
