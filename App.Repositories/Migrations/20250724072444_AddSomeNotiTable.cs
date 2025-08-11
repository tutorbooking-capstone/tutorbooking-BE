using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddSomeNotiTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_entities",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    notification_priority = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    additional_data = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_entities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app_user_notification",
                columns: table => new
                {
                    app_user_id = table.Column<string>(type: "text", nullable: false),
                    notification_entity_id = table.Column<string>(type: "text", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user_notification", x => new { x.app_user_id, x.notification_entity_id });
                    table.ForeignKey(
                        name: "fk_app_user_notification___users_app_user_id",
                        column: x => x.app_user_id,
                        principalTable: "__users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_app_user_notification__notification_entities_notification_ent~",
                        column: x => x.notification_entity_id,
                        principalTable: "notification_entities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_user_notification_notification_entity_id",
                table: "app_user_notification",
                column: "notification_entity_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_user_notification");

            migrationBuilder.DropTable(
                name: "notification_entities");
        }
    }
}
