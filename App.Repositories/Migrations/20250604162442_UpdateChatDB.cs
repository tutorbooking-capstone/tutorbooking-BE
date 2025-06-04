using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChatDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserConversations___users_AppUsersId",
                table: "UserConversations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserConversations_chat_conversations_ChatConversationId",
                table: "UserConversations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserConversations",
                table: "UserConversations");

            migrationBuilder.RenameTable(
                name: "UserConversations",
                newName: "user_conversations");

            migrationBuilder.RenameIndex(
                name: "IX_UserConversations_ChatConversationId",
                table: "user_conversations",
                newName: "IX_user_conversations_ChatConversationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_conversations",
                table: "user_conversations",
                columns: new[] { "AppUsersId", "ChatConversationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_user_conversations___users_AppUsersId",
                table: "user_conversations",
                column: "AppUsersId",
                principalTable: "__users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_conversations_chat_conversations_ChatConversationId",
                table: "user_conversations",
                column: "ChatConversationId",
                principalTable: "chat_conversations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_conversations___users_AppUsersId",
                table: "user_conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_user_conversations_chat_conversations_ChatConversationId",
                table: "user_conversations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_conversations",
                table: "user_conversations");

            migrationBuilder.RenameTable(
                name: "user_conversations",
                newName: "UserConversations");

            migrationBuilder.RenameIndex(
                name: "IX_user_conversations_ChatConversationId",
                table: "UserConversations",
                newName: "IX_UserConversations_ChatConversationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserConversations",
                table: "UserConversations",
                columns: new[] { "AppUsersId", "ChatConversationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserConversations___users_AppUsersId",
                table: "UserConversations",
                column: "AppUsersId",
                principalTable: "__users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserConversations_chat_conversations_ChatConversationId",
                table: "UserConversations",
                column: "ChatConversationId",
                principalTable: "chat_conversations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
