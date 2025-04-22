using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ConfigKeyForTutorHashtag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_tutor_hashtags",
                table: "tutor_hashtags");

            migrationBuilder.DropIndex(
                name: "IX_tutor_hashtags_tutor_id",
                table: "tutor_hashtags");

            migrationBuilder.DropColumn(
                name: "id",
                table: "tutor_hashtags");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "tutor_hashtags");

            migrationBuilder.DropColumn(
                name: "created_time",
                table: "tutor_hashtags");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "tutor_hashtags");

            migrationBuilder.DropColumn(
                name: "deleted_time",
                table: "tutor_hashtags");

            migrationBuilder.DropColumn(
                name: "last_updated_by",
                table: "tutor_hashtags");

            migrationBuilder.DropColumn(
                name: "last_updated_time",
                table: "tutor_hashtags");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tutor_hashtags",
                table: "tutor_hashtags",
                columns: new[] { "tutor_id", "hashtag_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_tutor_hashtags",
                table: "tutor_hashtags");

            migrationBuilder.AddColumn<string>(
                name: "id",
                table: "tutor_hashtags",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "tutor_hashtags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_time",
                table: "tutor_hashtags",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "tutor_hashtags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_time",
                table: "tutor_hashtags",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_updated_by",
                table: "tutor_hashtags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_updated_time",
                table: "tutor_hashtags",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddPrimaryKey(
                name: "pk_tutor_hashtags",
                table: "tutor_hashtags",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_tutor_hashtags_tutor_id",
                table: "tutor_hashtags",
                column: "tutor_id");
        }
    }
}
