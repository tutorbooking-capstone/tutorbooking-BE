using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableName : Migration
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
                newName: "AppUserChatConversation");

            migrationBuilder.RenameIndex(
                name: "IX_UserConversations_ChatConversationId",
                table: "AppUserChatConversation",
                newName: "IX_AppUserChatConversation_ChatConversationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppUserChatConversation",
                table: "AppUserChatConversation",
                columns: new[] { "AppUsersId", "ChatConversationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AppUserChatConversation___users_AppUsersId",
                table: "AppUserChatConversation",
                column: "AppUsersId",
                principalTable: "__users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppUserChatConversation_chat_conversations_ChatConversation~",
                table: "AppUserChatConversation",
                column: "ChatConversationId",
                principalTable: "chat_conversations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUserChatConversation___users_AppUsersId",
                table: "AppUserChatConversation");

            migrationBuilder.DropForeignKey(
                name: "FK_AppUserChatConversation_chat_conversations_ChatConversation~",
                table: "AppUserChatConversation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppUserChatConversation",
                table: "AppUserChatConversation");

            migrationBuilder.RenameTable(
                name: "AppUserChatConversation",
                newName: "UserConversations");

            migrationBuilder.RenameIndex(
                name: "IX_AppUserChatConversation_ChatConversationId",
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
