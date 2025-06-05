using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "chat_message_id",
                table: "file_upload",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "chat_conversations",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_conversations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    app_user_id = table.Column<string>(type: "text", nullable: false),
                    chat_conversation_id = table.Column<string>(type: "text", nullable: false),
                    text_message = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_chat_messages___users_app_user_id",
                        column: x => x.app_user_id,
                        principalTable: "__users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_chat_messages_chat_conversations_chat_conversation_id",
                        column: x => x.chat_conversation_id,
                        principalTable: "chat_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserConversations",
                columns: table => new
                {
                    AppUsersId = table.Column<string>(type: "text", nullable: false),
                    ChatConversationId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConversations", x => new { x.AppUsersId, x.ChatConversationId });
                    table.ForeignKey(
                        name: "FK_UserConversations___users_AppUsersId",
                        column: x => x.AppUsersId,
                        principalTable: "__users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserConversations_chat_conversations_ChatConversationId",
                        column: x => x.ChatConversationId,
                        principalTable: "chat_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_file_upload_chat_message_id",
                table: "file_upload",
                column: "chat_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_app_user_id",
                table: "chat_messages",
                column: "app_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_chat_conversation_id",
                table: "chat_messages",
                column: "chat_conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_UserConversations_ChatConversationId",
                table: "UserConversations",
                column: "ChatConversationId");

            migrationBuilder.AddForeignKey(
                name: "fk_file_upload_chat_messages_chat_message_id",
                table: "file_upload",
                column: "chat_message_id",
                principalTable: "chat_messages",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_file_upload_chat_messages_chat_message_id",
                table: "file_upload");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "UserConversations");

            migrationBuilder.DropTable(
                name: "chat_conversations");

            migrationBuilder.DropIndex(
                name: "ix_file_upload_chat_message_id",
                table: "file_upload");

            migrationBuilder.DropColumn(
                name: "chat_message_id",
                table: "file_upload");
        }
    }
}
