using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCascadeDeleteForTutorHashtag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id1",
                table: "tutor_hashtags");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_hashtags_hashtags_hashtag_id",
                table: "tutor_hashtags");

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id1",
                table: "tutor_hashtags",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_hashtags_hashtags_hashtag_id",
                table: "tutor_hashtags",
                column: "hashtag_id",
                principalTable: "hashtags",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id1",
                table: "tutor_hashtags");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_hashtags_hashtags_hashtag_id",
                table: "tutor_hashtags");

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id1",
                table: "tutor_hashtags",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_hashtags_hashtags_hashtag_id",
                table: "tutor_hashtags",
                column: "hashtag_id",
                principalTable: "hashtags",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
