using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAppliTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "verification_status",
                table: "tutors");

            migrationBuilder.RenameIndex(
                name: "ix_documents_application_id",
                table: "documents",
                newName: "IX_documents_application_id");

            migrationBuilder.RenameIndex(
                name: "ix_application_revisions_application_id",
                table: "application_revisions",
                newName: "IX_application_revisions_application_id");

            migrationBuilder.AddColumn<DateTime>(
                name: "assigned_at",
                table: "tutor_languages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "TutorApplicationId",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_verified",
                table: "documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TutorApplicationId",
                table: "application_revisions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_TutorApplicationId",
                table: "documents",
                column: "TutorApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_application_revisions_TutorApplicationId",
                table: "application_revisions",
                column: "TutorApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_application_revisions_tutor_applications_TutorApplicationId",
                table: "application_revisions",
                column: "TutorApplicationId",
                principalTable: "tutor_applications",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_documents_tutor_applications_TutorApplicationId",
                table: "documents",
                column: "TutorApplicationId",
                principalTable: "tutor_applications",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_application_revisions_tutor_applications_TutorApplicationId",
                table: "application_revisions");

            migrationBuilder.DropForeignKey(
                name: "FK_documents_tutor_applications_TutorApplicationId",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_TutorApplicationId",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_application_revisions_TutorApplicationId",
                table: "application_revisions");

            migrationBuilder.DropColumn(
                name: "assigned_at",
                table: "tutor_languages");

            migrationBuilder.DropColumn(
                name: "TutorApplicationId",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "is_verified",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "TutorApplicationId",
                table: "application_revisions");

            migrationBuilder.RenameIndex(
                name: "IX_documents_application_id",
                table: "documents",
                newName: "ix_documents_application_id");

            migrationBuilder.RenameIndex(
                name: "IX_application_revisions_application_id",
                table: "application_revisions",
                newName: "ix_application_revisions_application_id");

            migrationBuilder.AddColumn<int>(
                name: "verification_status",
                table: "tutors",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
