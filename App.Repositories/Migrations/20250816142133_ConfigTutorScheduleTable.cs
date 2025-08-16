using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ConfigTutorScheduleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "availability_slots");

            migrationBuilder.AddColumn<int>(
                name: "max_instant_booking_slots",
                table: "weekly_availability_patterns",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_instant_booking_slots",
                table: "weekly_availability_patterns");

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "availability_slots",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
