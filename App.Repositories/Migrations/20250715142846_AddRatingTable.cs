using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_booking_slots__learners_learner_temp_id",
                table: "booking_slots");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_slots__tutors_tutor_temp_id",
                table: "booking_slots");

            migrationBuilder.DropForeignKey(
                name: "fk_learner_time_slot_requests__learners_learner_temp_id1",
                table: "learner_time_slot_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_learner_time_slot_requests__tutors_tutor_temp_id1",
                table: "learner_time_slot_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_lessons__tutors_tutor_temp_id2",
                table: "lessons");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id3",
                table: "tutor_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_booking_offers__learners_learner_temp_id2",
                table: "tutor_booking_offers");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_booking_offers__tutors_tutor_temp_id4",
                table: "tutor_booking_offers");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id5",
                table: "tutor_hashtags");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id6",
                table: "tutor_languages");

            migrationBuilder.DropForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id7",
                table: "weekly_availability_patterns");

            migrationBuilder.AddColumn<string>(
                name: "booking_slot_rating_id",
                table: "booking_slots",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "booking_slot_ratings",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    booking_slot_id = table.Column<string>(type: "text", nullable: false),
                    tutor_id = table.Column<string>(type: "text", nullable: false),
                    learner_id = table.Column<string>(type: "text", nullable: false),
                    teaching_quality = table.Column<float>(type: "real", nullable: false),
                    attitude = table.Column<float>(type: "real", nullable: false),
                    commitment = table.Column<float>(type: "real", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_slot_ratings", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_slot_ratings__booking_slots_booking_slot_id",
                        column: x => x.booking_slot_id,
                        principalTable: "booking_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_booking_slot_ratings__learners_learner_temp_id",
                        column: x => x.learner_id,
                        principalTable: "learners",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_booking_slot_ratings__tutors_tutor_temp_id",
                        column: x => x.tutor_id,
                        principalTable: "tutors",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_booking_slot_ratings_booking_slot_id",
                table: "booking_slot_ratings",
                column: "booking_slot_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_slot_ratings_learner_id",
                table: "booking_slot_ratings",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_slot_ratings_tutor_id",
                table: "booking_slot_ratings",
                column: "tutor_id");

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slots__learners_learner_temp_id1",
                table: "booking_slots",
                column: "learner_id",
                principalTable: "learners",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slots__tutors_tutor_temp_id1",
                table: "booking_slots",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_booking_slots__learners_learner_temp_id1",
                table: "booking_slots");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_slots__tutors_tutor_temp_id1",
                table: "booking_slots");

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

            migrationBuilder.DropTable(
                name: "booking_slot_ratings");

            migrationBuilder.DropColumn(
                name: "booking_slot_rating_id",
                table: "booking_slots");

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slots__learners_learner_temp_id",
                table: "booking_slots",
                column: "learner_id",
                principalTable: "learners",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slots__tutors_tutor_temp_id",
                table: "booking_slots",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_learner_time_slot_requests__learners_learner_temp_id1",
                table: "learner_time_slot_requests",
                column: "learner_id",
                principalTable: "learners",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_learner_time_slot_requests__tutors_tutor_temp_id1",
                table: "learner_time_slot_requests",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_lessons__tutors_tutor_temp_id2",
                table: "lessons",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id3",
                table: "tutor_applications",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_booking_offers__learners_learner_temp_id2",
                table: "tutor_booking_offers",
                column: "learner_id",
                principalTable: "learners",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_booking_offers__tutors_tutor_temp_id4",
                table: "tutor_booking_offers",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id5",
                table: "tutor_hashtags",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id6",
                table: "tutor_languages",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id7",
                table: "weekly_availability_patterns",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
