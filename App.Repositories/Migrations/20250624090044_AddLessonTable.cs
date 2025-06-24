using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id1",
                table: "tutor_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id2",
                table: "tutor_hashtags");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id3",
                table: "tutor_languages");

            migrationBuilder.DropForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id4",
                table: "weekly_availability_patterns");

            migrationBuilder.CreateTable(
                name: "lessons",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    target_audience = table.Column<string>(type: "text", nullable: false),
                    prerequisites = table.Column<string>(type: "text", nullable: false),
                    language_code = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    duration_in_minutes = table.Column<int>(type: "integer", nullable: false),
                    tutor_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lessons", x => x.id);
                    table.ForeignKey(
                        name: "fk_lessons__tutors_tutor_temp_id1",
                        column: x => x.tutor_id,
                        principalTable: "tutors",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lessons_tutor_id",
                table: "lessons",
                column: "tutor_id");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropTable(
                name: "lessons");

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id1",
                table: "tutor_applications",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id2",
                table: "tutor_hashtags",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id3",
                table: "tutor_languages",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id4",
                table: "weekly_availability_patterns",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
