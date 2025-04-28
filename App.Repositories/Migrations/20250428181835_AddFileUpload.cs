using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddFileUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cloudinary_url",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "content_type",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "file_size",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "uploaded_at",
                table: "documents");

            migrationBuilder.CreateTable(
                name: "file_upload",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    cloudinary_url = table.Column<string>(type: "text", nullable: false),
                    original_file_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_upload", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_file_uploads",
                columns: table => new
                {
                    document_id = table.Column<string>(type: "text", nullable: false),
                    file_upload_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_file_uploads", x => new { x.document_id, x.file_upload_id });
                    table.ForeignKey(
                        name: "fk_document_file_uploads__file_upload_file_upload_id",
                        column: x => x.file_upload_id,
                        principalTable: "file_upload",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_document_file_uploads_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_document_file_uploads_file_upload_id",
                table: "document_file_uploads",
                column: "file_upload_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_file_uploads");

            migrationBuilder.DropTable(
                name: "file_upload");

            migrationBuilder.AddColumn<string>(
                name: "cloudinary_url",
                table: "documents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "content_type",
                table: "documents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "file_size",
                table: "documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "uploaded_at",
                table: "documents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
