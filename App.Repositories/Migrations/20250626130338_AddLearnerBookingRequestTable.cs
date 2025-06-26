using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddLearnerBookingRequestTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_lessons__tutors_tutor_temp_id1",
                table: "lessons");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id2",
                table: "tutor_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id3",
                table: "tutor_hashtags");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id4",
                table: "tutor_languages");

            migrationBuilder.DropForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id5",
                table: "weekly_availability_patterns");

            migrationBuilder.CreateTable(
                name: "learner_time_slot_requests",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    learner_id = table.Column<string>(type: "text", nullable: false),
                    tutor_id = table.Column<string>(type: "text", nullable: false),
                    day_in_week = table.Column<int>(type: "integer", nullable: false),
                    slot_index = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learner_time_slot_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_learner_time_slot_requests__learners_learner_temp_id1",
                        column: x => x.learner_id,
                        principalTable: "learners",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_learner_time_slot_requests__tutors_tutor_temp_id1",
                        column: x => x.tutor_id,
                        principalTable: "tutors",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_learner_time_slot_requests_learner_id_tutor_id_day_in_week_~",
                table: "learner_time_slot_requests",
                columns: new[] { "learner_id", "tutor_id", "day_in_week", "slot_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learner_time_slot_requests_tutor_id",
                table: "learner_time_slot_requests",
                column: "tutor_id");

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
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id4",
                table: "tutor_hashtags",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id5",
                table: "tutor_languages",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id6",
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
                name: "fk_lessons__tutors_tutor_temp_id2",
                table: "lessons");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id3",
                table: "tutor_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id4",
                table: "tutor_hashtags");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id5",
                table: "tutor_languages");

            migrationBuilder.DropForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id6",
                table: "weekly_availability_patterns");

            migrationBuilder.DropTable(
                name: "learner_time_slot_requests");

            migrationBuilder.AddForeignKey(
                name: "fk_lessons__tutors_tutor_temp_id1",
                table: "lessons",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id2",
                table: "tutor_applications",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id3",
                table: "tutor_hashtags",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id4",
                table: "tutor_languages",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id5",
                table: "weekly_availability_patterns",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
