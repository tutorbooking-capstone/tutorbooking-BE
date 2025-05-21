using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePictureColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "profile_picture_public_id",
                table: "__users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "profile_picture_url",
                table: "__users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profile_picture_public_id",
                table: "__users");

            migrationBuilder.DropColumn(
                name: "profile_picture_url",
                table: "__users");
        }
    }
}
