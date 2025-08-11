using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddDisputeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_booking_slot_ratings__learners_learner_temp_id1",
                table: "booking_slot_ratings");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_slot_ratings__tutors_tutor_temp_id1",
                table: "booking_slot_ratings");

            migrationBuilder.DropForeignKey(
                name: "fk_bookings__learners_learner_temp_id",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_bookings__tutors_tutor_temp_id",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_learner_time_slot_requests__learners_learner_temp_id2",
                table: "learner_time_slot_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_learner_time_slot_requests__tutors_tutor_temp_id2",
                table: "learner_time_slot_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_lessons__tutors_tutor_temp_id3",
                table: "lessons");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id4",
                table: "tutor_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_booking_offers__learners_learner_temp_id3",
                table: "tutor_booking_offers");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_booking_offers__tutors_tutor_temp_id5",
                table: "tutor_booking_offers");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id6",
                table: "tutor_hashtags");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id7",
                table: "tutor_languages");

            migrationBuilder.DropForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id8",
                table: "weekly_availability_patterns");

            migrationBuilder.AddColumn<string>(
                name: "current_dispute_id",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "dispute_id",
                table: "booked_slots",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "booking_disputes",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    booking_id = table.Column<string>(type: "text", nullable: false),
                    learner_id = table.Column<string>(type: "text", nullable: false),
                    tutor_id = table.Column<string>(type: "text", nullable: false),
                    staff_id = table.Column<string>(type: "text", nullable: true),
                    case_number = table.Column<string>(type: "text", nullable: false),
                    learner_reason = table.Column<string>(type: "text", nullable: false),
                    tutor_response = table.Column<string>(type: "text", nullable: true),
                    staff_notes = table.Column<string>(type: "text", nullable: true),
                    evidence_urls = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    resolution = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reconciliation_end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tutor_responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    staff_review_end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_disputes", x => x.id);
                    table.ForeignKey(
                        name: "FK_booking_disputes_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_disputes___users_staff_id",
                        column: x => x.staff_id,
                        principalTable: "__users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_booking_disputes__learners_learner_temp_id",
                        column: x => x.learner_id,
                        principalTable: "learners",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_booking_disputes__tutors_tutor_temp_id",
                        column: x => x.tutor_id,
                        principalTable: "tutors",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_current_dispute_id",
                table: "bookings",
                column: "current_dispute_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_booked_slots_dispute_id",
                table: "booked_slots",
                column: "dispute_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_disputes_booking_id",
                table: "booking_disputes",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_disputes_learner_id",
                table: "booking_disputes",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_disputes_staff_id",
                table: "booking_disputes",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_disputes_tutor_id",
                table: "booking_disputes",
                column: "tutor_id");

            migrationBuilder.AddForeignKey(
                name: "fk_booked_slots_booking_disputes_dispute_id",
                table: "booked_slots",
                column: "dispute_id",
                principalTable: "booking_disputes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slot_ratings__learners_learner_temp_id2",
                table: "booking_slot_ratings",
                column: "learner_id",
                principalTable: "learners",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slot_ratings__tutors_tutor_temp_id2",
                table: "booking_slot_ratings",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_booking_disputes_current_dispute_id",
                table: "bookings",
                column: "current_dispute_id",
                principalTable: "booking_disputes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_bookings__learners_learner_temp_id1",
                table: "bookings",
                column: "learner_id",
                principalTable: "learners",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_bookings__tutors_tutor_temp_id1",
                table: "bookings",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_learner_time_slot_requests__learners_learner_temp_id3",
                table: "learner_time_slot_requests",
                column: "learner_id",
                principalTable: "learners",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_learner_time_slot_requests__tutors_tutor_temp_id3",
                table: "learner_time_slot_requests",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_lessons__tutors_tutor_temp_id4",
                table: "lessons",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id5",
                table: "tutor_applications",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_booking_offers__learners_learner_temp_id4",
                table: "tutor_booking_offers",
                column: "learner_id",
                principalTable: "learners",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_booking_offers__tutors_tutor_temp_id6",
                table: "tutor_booking_offers",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id7",
                table: "tutor_hashtags",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id8",
                table: "tutor_languages",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id9",
                table: "weekly_availability_patterns",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_booked_slots_booking_disputes_dispute_id",
                table: "booked_slots");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_slot_ratings__learners_learner_temp_id2",
                table: "booking_slot_ratings");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_slot_ratings__tutors_tutor_temp_id2",
                table: "booking_slot_ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_bookings_booking_disputes_current_dispute_id",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_bookings__learners_learner_temp_id1",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_bookings__tutors_tutor_temp_id1",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_learner_time_slot_requests__learners_learner_temp_id3",
                table: "learner_time_slot_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_learner_time_slot_requests__tutors_tutor_temp_id3",
                table: "learner_time_slot_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_lessons__tutors_tutor_temp_id4",
                table: "lessons");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id5",
                table: "tutor_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_booking_offers__learners_learner_temp_id4",
                table: "tutor_booking_offers");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_booking_offers__tutors_tutor_temp_id6",
                table: "tutor_booking_offers");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id7",
                table: "tutor_hashtags");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id8",
                table: "tutor_languages");

            migrationBuilder.DropForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id9",
                table: "weekly_availability_patterns");

            migrationBuilder.DropTable(
                name: "booking_disputes");

            migrationBuilder.DropIndex(
                name: "IX_bookings_current_dispute_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_booked_slots_dispute_id",
                table: "booked_slots");

            migrationBuilder.DropColumn(
                name: "current_dispute_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "status",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "dispute_id",
                table: "booked_slots");

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
                name: "fk_bookings__learners_learner_temp_id",
                table: "bookings",
                column: "learner_id",
                principalTable: "learners",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_bookings__tutors_tutor_temp_id",
                table: "bookings",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_learner_time_slot_requests__learners_learner_temp_id2",
                table: "learner_time_slot_requests",
                column: "learner_id",
                principalTable: "learners",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_learner_time_slot_requests__tutors_tutor_temp_id2",
                table: "learner_time_slot_requests",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_lessons__tutors_tutor_temp_id3",
                table: "lessons",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id4",
                table: "tutor_applications",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_booking_offers__learners_learner_temp_id3",
                table: "tutor_booking_offers",
                column: "learner_id",
                principalTable: "learners",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_booking_offers__tutors_tutor_temp_id5",
                table: "tutor_booking_offers",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id6",
                table: "tutor_hashtags",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id7",
                table: "tutor_languages",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id8",
                table: "weekly_availability_patterns",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
