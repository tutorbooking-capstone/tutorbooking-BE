using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class TutorApplicationRelationshipChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_documents_application_id",
                table: "documents",
                newName: "ix_documents_application_id");

            migrationBuilder.RenameIndex(
                name: "IX_application_revisions_application_id",
                table: "application_revisions",
                newName: "ix_application_revisions_application_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_documents_application_id",
                table: "documents",
                newName: "IX_documents_application_id");

            migrationBuilder.RenameIndex(
                name: "ix_application_revisions_application_id",
                table: "application_revisions",
                newName: "IX_application_revisions_application_id");
        }
    }
}
