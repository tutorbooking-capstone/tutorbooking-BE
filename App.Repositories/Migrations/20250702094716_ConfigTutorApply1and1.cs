using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ConfigTutorApply1and1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id3",
                table: "tutor_applications");

            migrationBuilder.DropIndex(
                name: "IX_tutor_applications_tutor_id",
                table: "tutor_applications");

            migrationBuilder.AddColumn<string>(
                name: "TutorUserId",
                table: "tutor_applications",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tutor_applications_tutor_id",
                table: "tutor_applications",
                column: "tutor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tutor_applications_TutorUserId",
                table: "tutor_applications",
                column: "TutorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_tutor_applications_tutors_TutorUserId",
                table: "tutor_applications",
                column: "TutorUserId",
                principalTable: "tutors",
                principalColumn: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id3",
                table: "tutor_applications",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tutor_applications_tutors_TutorUserId",
                table: "tutor_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id3",
                table: "tutor_applications");

            migrationBuilder.DropIndex(
                name: "IX_tutor_applications_tutor_id",
                table: "tutor_applications");

            migrationBuilder.DropIndex(
                name: "IX_tutor_applications_TutorUserId",
                table: "tutor_applications");

            migrationBuilder.DropColumn(
                name: "TutorUserId",
                table: "tutor_applications");

            migrationBuilder.CreateIndex(
                name: "IX_tutor_applications_tutor_id",
                table: "tutor_applications",
                column: "tutor_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id3",
                table: "tutor_applications",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
