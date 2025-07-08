using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLearnerRequestTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_learner_time_slot_requests_learner_id_tutor_id_day_in_week_~",
                table: "learner_time_slot_requests");

            migrationBuilder.DropColumn(
                name: "day_in_week",
                table: "learner_time_slot_requests");

            migrationBuilder.DropColumn(
                name: "slot_index",
                table: "learner_time_slot_requests");

            migrationBuilder.AddColumn<DateTime>(
                name: "expected_start_date",
                table: "learner_time_slot_requests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "lesson_id",
                table: "learner_time_slot_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "requested_slots_json",
                table: "learner_time_slot_requests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_learner_time_slot_requests_learner_id_tutor_id",
                table: "learner_time_slot_requests",
                columns: new[] { "learner_id", "tutor_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_learner_time_slot_requests_lesson_id",
                table: "learner_time_slot_requests",
                column: "lesson_id");

            migrationBuilder.AddForeignKey(
                name: "fk_learner_time_slot_requests__lessons_lesson_id",
                table: "learner_time_slot_requests",
                column: "lesson_id",
                principalTable: "lessons",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_learner_time_slot_requests__lessons_lesson_id",
                table: "learner_time_slot_requests");

            migrationBuilder.DropIndex(
                name: "IX_learner_time_slot_requests_learner_id_tutor_id",
                table: "learner_time_slot_requests");

            migrationBuilder.DropIndex(
                name: "ix_learner_time_slot_requests_lesson_id",
                table: "learner_time_slot_requests");

            migrationBuilder.DropColumn(
                name: "expected_start_date",
                table: "learner_time_slot_requests");

            migrationBuilder.DropColumn(
                name: "lesson_id",
                table: "learner_time_slot_requests");

            migrationBuilder.DropColumn(
                name: "requested_slots_json",
                table: "learner_time_slot_requests");

            migrationBuilder.AddColumn<int>(
                name: "day_in_week",
                table: "learner_time_slot_requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "slot_index",
                table: "learner_time_slot_requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_learner_time_slot_requests_learner_id_tutor_id_day_in_week_~",
                table: "learner_time_slot_requests",
                columns: new[] { "learner_id", "tutor_id", "day_in_week", "slot_index" },
                unique: true);
        }
    }
}
