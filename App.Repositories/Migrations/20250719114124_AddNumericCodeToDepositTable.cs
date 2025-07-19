using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddNumericCodeToDepositTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "numeric_order_code",
                table: "deposit_requests",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_deposit_requests_numeric_order_code",
                table: "deposit_requests",
                column: "numeric_order_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deposit_requests_numeric_order_code",
                table: "deposit_requests");

            migrationBuilder.DropColumn(
                name: "numeric_order_code",
                table: "deposit_requests");
        }
    }
}
