using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddChatStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_conversation_read_statuses",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    chat_conversation_id = table.Column<string>(type: "text", nullable: false),
                    last_read_chat_message_id = table.Column<string>(type: "text", nullable: false),
                    last_read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_conversation_read_statuses", x => x.id);
                    table.ForeignKey(
                        name: "fk_chat_conversation_read_statuses___users_app_user_id",
                        column: x => x.user_id,
                        principalTable: "__users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_chat_conversation_read_statuses__chat_messages_last_read_chat~",
                        column: x => x.last_read_chat_message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_chat_conversation_read_statuses_chat_conversations_chat_con~",
                        column: x => x.chat_conversation_id,
                        principalTable: "chat_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chat_conversation_read_statuses_chat_conversation_id",
                table: "chat_conversation_read_statuses",
                column: "chat_conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_conversation_read_statuses_last_read_chat_message_id",
                table: "chat_conversation_read_statuses",
                column: "last_read_chat_message_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversation_read_statuses_user_id",
                table: "chat_conversation_read_statuses",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_conversation_read_statuses");
        }
    }
}
