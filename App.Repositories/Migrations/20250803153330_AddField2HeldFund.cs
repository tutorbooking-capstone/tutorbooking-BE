using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddField2HeldFund : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "release_at",
                table: "held_funds",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "booked_slot_id",
                table: "held_funds",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "held_funds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "withdrawal_request_id",
                table: "held_funds",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_held_funds_withdrawal_request_id",
                table: "held_funds",
                column: "withdrawal_request_id");

            migrationBuilder.AddForeignKey(
                name: "fk_held_funds__withdrawal_requests_withdrawal_request_id",
                table: "held_funds",
                column: "withdrawal_request_id",
                principalTable: "withdrawal_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_held_funds__withdrawal_requests_withdrawal_request_id",
                table: "held_funds");

            migrationBuilder.DropIndex(
                name: "ix_held_funds_withdrawal_request_id",
                table: "held_funds");

            migrationBuilder.DropColumn(
                name: "type",
                table: "held_funds");

            migrationBuilder.DropColumn(
                name: "withdrawal_request_id",
                table: "held_funds");

            migrationBuilder.AlterColumn<DateTime>(
                name: "release_at",
                table: "held_funds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "booked_slot_id",
                table: "held_funds",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
