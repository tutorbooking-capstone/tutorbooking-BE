using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_booking_disputes__tutors_tutor_temp_id",
                table: "booking_disputes");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_slot_ratings__tutors_tutor_temp_id2",
                table: "booking_slot_ratings");

            migrationBuilder.DropForeignKey(
                name: "fk_bookings__tutors_tutor_temp_id1",
                table: "bookings");

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
                name: "fk_tutor_booking_offers__tutors_tutor_temp_id6",
                table: "tutor_booking_offers");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id7",
                table: "tutor_hashtags");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_introduction_videos__tutors_tutor_temp_id8",
                table: "tutor_introduction_videos");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id9",
                table: "tutor_languages");

            migrationBuilder.DropForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id10",
                table: "weekly_availability_patterns");

            migrationBuilder.DropColumn(
                name: "max_instant_booking_slots",
                table: "weekly_availability_patterns");

            migrationBuilder.CreateTable(
                name: "booking_configs",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tutor_id = table.Column<string>(type: "text", nullable: false),
                    allow_instant_booking = table.Column<bool>(type: "boolean", nullable: false),
                    max_instant_booking_slots = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_configs", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_configs__tutors_tutor_temp_id",
                        column: x => x.tutor_id,
                        principalTable: "tutors",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_configs_tutor_id",
                table: "booking_configs",
                column: "tutor_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_disputes__tutors_tutor_temp_id1",
                table: "booking_disputes",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slot_ratings__tutors_tutor_temp_id3",
                table: "booking_slot_ratings",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_bookings__tutors_tutor_temp_id2",
                table: "bookings",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_learner_time_slot_requests__tutors_tutor_temp_id4",
                table: "learner_time_slot_requests",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_lessons__tutors_tutor_temp_id5",
                table: "lessons",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id6",
                table: "tutor_applications",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_booking_offers__tutors_tutor_temp_id7",
                table: "tutor_booking_offers",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id8",
                table: "tutor_hashtags",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_introduction_videos__tutors_tutor_temp_id9",
                table: "tutor_introduction_videos",
                column: "tutor_user_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id10",
                table: "tutor_languages",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id11",
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
                name: "fk_booking_disputes__tutors_tutor_temp_id1",
                table: "booking_disputes");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_slot_ratings__tutors_tutor_temp_id3",
                table: "booking_slot_ratings");

            migrationBuilder.DropForeignKey(
                name: "fk_bookings__tutors_tutor_temp_id2",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_learner_time_slot_requests__tutors_tutor_temp_id4",
                table: "learner_time_slot_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_lessons__tutors_tutor_temp_id5",
                table: "lessons");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id6",
                table: "tutor_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_booking_offers__tutors_tutor_temp_id7",
                table: "tutor_booking_offers");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id8",
                table: "tutor_hashtags");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_introduction_videos__tutors_tutor_temp_id9",
                table: "tutor_introduction_videos");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id10",
                table: "tutor_languages");

            migrationBuilder.DropForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id11",
                table: "weekly_availability_patterns");

            migrationBuilder.DropTable(
                name: "booking_configs");

            migrationBuilder.AddColumn<int>(
                name: "max_instant_booking_slots",
                table: "weekly_availability_patterns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_disputes__tutors_tutor_temp_id",
                table: "booking_disputes",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slot_ratings__tutors_tutor_temp_id2",
                table: "booking_slot_ratings",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_bookings__tutors_tutor_temp_id1",
                table: "bookings",
                column: "tutor_id",
                principalTable: "tutors",
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
                name: "fk_tutor_introduction_videos__tutors_tutor_temp_id8",
                table: "tutor_introduction_videos",
                column: "tutor_user_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id9",
                table: "tutor_languages",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id10",
                table: "weekly_availability_patterns",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
