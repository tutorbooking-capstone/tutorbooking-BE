using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityForChedulingFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_document_file_uploads_documents_document_id",
                table: "document_file_uploads");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id",
                table: "tutor_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id1",
                table: "tutor_hashtags");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id2",
                table: "tutor_languages");

            migrationBuilder.AddColumn<string>(
                name: "hardcopy_submit_id",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "hardcopy_submits",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    application_id = table.Column<string>(type: "text", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    staff_notes = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hardcopy_submits", x => x.id);
                    table.ForeignKey(
                        name: "fk_hardcopy_submits__tutor_applications_application_id",
                        column: x => x.application_id,
                        principalTable: "tutor_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "learners",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learners", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_learners___users_user_id",
                        column: x => x.user_id,
                        principalTable: "__users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_availability_patterns",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tutor_id = table.Column<string>(type: "text", nullable: false),
                    applied_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_availability_patterns", x => x.id);
                    table.ForeignKey(
                        name: "fk_weekly_availability_patterns__tutors_tutor_temp_id4",
                        column: x => x.tutor_id,
                        principalTable: "tutors",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_slots",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tutor_id = table.Column<string>(type: "text", nullable: false),
                    learner_id = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    repeat_for_weeks = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_slots", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_slots__learners_learner_temp_id",
                        column: x => x.learner_id,
                        principalTable: "learners",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_booking_slots__tutors_tutor_temp_id",
                        column: x => x.tutor_id,
                        principalTable: "tutors",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "availability_slots",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    day_in_week = table.Column<int>(type: "integer", nullable: false),
                    slot_index = table.Column<int>(type: "integer", nullable: false),
                    booking_slot_id = table.Column<string>(type: "text", nullable: true),
                    weekly_pattern_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_availability_slots", x => x.id);
                    table.ForeignKey(
                        name: "fk_availability_slots__booking_slots_booking_slot_id",
                        column: x => x.booking_slot_id,
                        principalTable: "booking_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_availability_slots__weekly_availability_patterns_weekly_patter~",
                        column: x => x.weekly_pattern_id,
                        principalTable: "weekly_availability_patterns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_documents_hardcopy_submit_id",
                table: "documents",
                column: "hardcopy_submit_id");

            migrationBuilder.CreateIndex(
                name: "ix_availability_slots_booking_slot_id",
                table: "availability_slots",
                column: "booking_slot_id");

            migrationBuilder.CreateIndex(
                name: "ix_availability_slots_weekly_pattern_id",
                table: "availability_slots",
                column: "weekly_pattern_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_slots_learner_id",
                table: "booking_slots",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_slots_tutor_id",
                table: "booking_slots",
                column: "tutor_id");

            migrationBuilder.CreateIndex(
                name: "ix_hardcopy_submits_application_id",
                table: "hardcopy_submits",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "IX_weekly_availability_patterns_tutor_id",
                table: "weekly_availability_patterns",
                column: "tutor_id");

            migrationBuilder.AddForeignKey(
                name: "fk_document_file_uploads__documents_document_id",
                table: "document_file_uploads",
                column: "document_id",
                principalTable: "documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_documents__hardcopy_submits_hardcopy_submit_id",
                table: "documents",
                column: "hardcopy_submit_id",
                principalTable: "hardcopy_submits",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_document_file_uploads__documents_document_id",
                table: "document_file_uploads");

            migrationBuilder.DropForeignKey(
                name: "fk_documents__hardcopy_submits_hardcopy_submit_id",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id1",
                table: "tutor_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id2",
                table: "tutor_hashtags");

            migrationBuilder.DropForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id3",
                table: "tutor_languages");

            migrationBuilder.DropTable(
                name: "availability_slots");

            migrationBuilder.DropTable(
                name: "hardcopy_submits");

            migrationBuilder.DropTable(
                name: "booking_slots");

            migrationBuilder.DropTable(
                name: "weekly_availability_patterns");

            migrationBuilder.DropTable(
                name: "learners");

            migrationBuilder.DropIndex(
                name: "ix_documents_hardcopy_submit_id",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "hardcopy_submit_id",
                table: "documents");

            migrationBuilder.AddForeignKey(
                name: "fk_document_file_uploads_documents_document_id",
                table: "document_file_uploads",
                column: "document_id",
                principalTable: "documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_applications__tutors_tutor_temp_id",
                table: "tutor_applications",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_hashtags__tutors_tutor_temp_id1",
                table: "tutor_hashtags",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tutor_languages__tutors_tutor_temp_id2",
                table: "tutor_languages",
                column: "tutor_id",
                principalTable: "tutors",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
