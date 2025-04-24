using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFirebaseToCloudinary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "download_url",
                table: "documents");

            migrationBuilder.RenameColumn(
                name: "storage_path",
                table: "documents",
                newName: "cloudinary_url");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "cloudinary_url",
                table: "documents",
                newName: "storage_path");

            migrationBuilder.AddColumn<string>(
                name: "download_url",
                table: "documents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
