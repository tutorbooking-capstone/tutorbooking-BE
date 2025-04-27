using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTutorLanguageCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id2",
                table: "tutor_languages");

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id2",
                table: "tutor_languages",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id2",
                table: "tutor_languages");

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id2",
                table: "tutor_languages",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
