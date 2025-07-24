using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ChangeBookingNameTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_booked_slots__booking_slots_booking_slot_id",
                table: "booked_slots");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_slot_ratings__booking_slots_booking_slot_id",
                table: "booking_slot_ratings");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_slot_ratings__learners_learner_temp_id",
                table: "booking_slot_ratings");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_slot_ratings__tutors_tutor_temp_id",
                table: "booking_slot_ratings");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_booking_offers__lessons_lesson_id",
                table: "tutor_booking_offers");

            migrationBuilder.DropTable(
                name: "booking_slots");

            migrationBuilder.DropColumn(
                name: "total_price",
                table: "tutor_booking_offers");

            migrationBuilder.RenameColumn(
                name: "booking_slot_id",
                table: "booking_slot_ratings",
                newName: "booking_id");

            migrationBuilder.RenameIndex(
                name: "ix_booking_slot_ratings_booking_slot_id",
                table: "booking_slot_ratings",
                newName: "ix_booking_slot_ratings_booking_id");

            migrationBuilder.RenameColumn(
                name: "booking_slot_id",
                table: "booked_slots",
                newName: "booking_id");

            migrationBuilder.RenameIndex(
                name: "ix_booked_slots_booking_slot_id",
                table: "booked_slots",
                newName: "ix_booked_slots_booking_id");

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tutor_id = table.Column<string>(type: "text", nullable: false),
                    learner_id = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    booking_slot_rating_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                    table.ForeignKey(
                        name: "fk_bookings__learners_learner_temp_id",
                        column: x => x.learner_id,
                        principalTable: "learners",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_bookings__tutors_tutor_temp_id",
                        column: x => x.tutor_id,
                        principalTable: "tutors",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_learner_id",
                table: "bookings",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_tutor_id",
                table: "bookings",
                column: "tutor_id");

            migrationBuilder.AddForeignKey(
                name: "fk_booked_slots_bookings_booking_id",
                table: "booked_slots",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slot_ratings__learners_learner_temp_id1",
                table: "booking_slot_ratings",
                column: "learner_id",
                principalTable: "learners",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slot_ratings__tutors_tutor_temp_id1",
                table: "booking_slot_ratings",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slot_ratings_bookings_booking_id",
                table: "booking_slot_ratings",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_booking_offers_lessons_lesson_id",
                table: "tutor_booking_offers",
                column: "lesson_id",
                principalTable: "lessons",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_booked_slots_bookings_booking_id",
                table: "booked_slots");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_slot_ratings__learners_learner_temp_id1",
                table: "booking_slot_ratings");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_slot_ratings__tutors_tutor_temp_id1",
                table: "booking_slot_ratings");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_slot_ratings_bookings_booking_id",
                table: "booking_slot_ratings");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_booking_offers_lessons_lesson_id",
                table: "tutor_booking_offers");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.RenameColumn(
                name: "booking_id",
                table: "booking_slot_ratings",
                newName: "booking_slot_id");

            migrationBuilder.RenameIndex(
                name: "ix_booking_slot_ratings_booking_id",
                table: "booking_slot_ratings",
                newName: "ix_booking_slot_ratings_booking_slot_id");

            migrationBuilder.RenameColumn(
                name: "booking_id",
                table: "booked_slots",
                newName: "booking_slot_id");

            migrationBuilder.RenameIndex(
                name: "ix_booked_slots_booking_id",
                table: "booked_slots",
                newName: "ix_booked_slots_booking_slot_id");

            migrationBuilder.AddColumn<decimal>(
                name: "total_price",
                table: "tutor_booking_offers",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "booking_slots",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    learner_id = table.Column<string>(type: "text", nullable: true),
                    tutor_id = table.Column<string>(type: "text", nullable: false),
                    booking_slot_rating_id = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_slots", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_slots__learners_learner_temp_id1",
                        column: x => x.learner_id,
                        principalTable: "learners",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_booking_slots__tutors_tutor_temp_id1",
                        column: x => x.tutor_id,
                        principalTable: "tutors",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_slots_learner_id",
                table: "booking_slots",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_slots_tutor_id",
                table: "booking_slots",
                column: "tutor_id");

            migrationBuilder.AddForeignKey(
                name: "fk_booked_slots__booking_slots_booking_slot_id",
                table: "booked_slots",
                column: "booking_slot_id",
                principalTable: "booking_slots",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slot_ratings__booking_slots_booking_slot_id",
                table: "booking_slot_ratings",
                column: "booking_slot_id",
                principalTable: "booking_slots",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slot_ratings__learners_learner_temp_id",
                table: "booking_slot_ratings",
                column: "learner_id",
                principalTable: "learners",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slot_ratings__tutors_tutor_temp_id",
                table: "booking_slot_ratings",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_booking_offers__lessons_lesson_id",
                table: "tutor_booking_offers",
                column: "lesson_id",
                principalTable: "lessons",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
