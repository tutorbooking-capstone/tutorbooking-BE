using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddConfig4StaffAndManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "encrypted_citizen_id",
                table: "staffs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "managers",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    encrypted_citizen_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_managers", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_managers___users_user_id",
                        column: x => x.user_id,
                        principalTable: "__users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "managers");

            migrationBuilder.DropColumn(
                name: "encrypted_citizen_id",
                table: "staffs");
        }
    }
}
