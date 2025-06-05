using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceCheckTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_conversations",
                columns: table => new
                {
                    AppUsersId = table.Column<string>(type: "text", nullable: false),
                    ChatConversationId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_conversations", x => new { x.AppUsersId, x.ChatConversationId });
                    table.ForeignKey(
                        name: "FK_user_conversations___users_AppUsersId",
                        column: x => x.AppUsersId,
                        principalTable: "__users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_conversations_chat_conversations_ChatConversationId",
                        column: x => x.ChatConversationId,
                        principalTable: "chat_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_conversations_ChatConversationId",
                table: "user_conversations",
                column: "ChatConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_conversations");
        }
    }
}
