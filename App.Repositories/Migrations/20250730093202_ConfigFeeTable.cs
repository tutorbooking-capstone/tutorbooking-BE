using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ConfigFeeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "fee_configs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_time",
                table: "fee_configs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "fee_configs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_time",
                table: "fee_configs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "effective_from",
                table: "fee_configs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "effective_to",
                table: "fee_configs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_updated_by",
                table: "fee_configs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_updated_time",
                table: "fee_configs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                table: "fee_configs");

            migrationBuilder.DropColumn(
                name: "created_time",
                table: "fee_configs");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "fee_configs");

            migrationBuilder.DropColumn(
                name: "deleted_time",
                table: "fee_configs");

            migrationBuilder.DropColumn(
                name: "effective_from",
                table: "fee_configs");

            migrationBuilder.DropColumn(
                name: "effective_to",
                table: "fee_configs");

            migrationBuilder.DropColumn(
                name: "last_updated_by",
                table: "fee_configs");

            migrationBuilder.DropColumn(
                name: "last_updated_time",
                table: "fee_configs");
        }
    }
}
