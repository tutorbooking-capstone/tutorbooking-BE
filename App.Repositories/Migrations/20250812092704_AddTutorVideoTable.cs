using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorVideoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id8",
                table: "tutor_languages");

            migrationBuilder.DropForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id9",
                table: "weekly_availability_patterns");

            migrationBuilder.CreateTable(
                name: "tutor_introduction_videos",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tutor_user_id = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tutor_introduction_videos", x => x.id);
                    table.ForeignKey(
                        name: "fk_tutor_introduction_videos__tutors_tutor_temp_id8",
                        column: x => x.tutor_user_id,
                        principalTable: "tutors",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tutor_introduction_videos_tutor_user_id",
                table: "tutor_introduction_videos",
                column: "tutor_user_id");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id9",
                table: "tutor_languages");

            migrationBuilder.DropForeignKey(
                name: "fk_weekly_availability_patterns__tutors_tutor_temp_id10",
                table: "weekly_availability_patterns");

            migrationBuilder.DropTable(
                name: "tutor_introduction_videos");

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
    }
}
