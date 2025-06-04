using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorFlowEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_blogs_i_users_app_user_id",
                table: "blogs");

            migrationBuilder.DropForeignKey(
                name: "fk_i_role_claims_i_roles_role_id",
                table: "i_role_claims");

            migrationBuilder.DropForeignKey(
                name: "fk_i_user_claims_i_users_user_id",
                table: "i_user_claims");

            migrationBuilder.DropForeignKey(
                name: "fk_i_user_logins_i_users_user_id",
                table: "i_user_logins");

            migrationBuilder.DropForeignKey(
                name: "fk_i_user_roles_i_roles_role_id",
                table: "i_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_i_user_roles_i_users_user_id",
                table: "i_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_i_user_tokens_i_users_user_id",
                table: "i_user_tokens");

            migrationBuilder.DropIndex(
                name: "IX_blogs_user_id",
                table: "blogs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_i_users",
                table: "i_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_i_user_tokens",
                table: "i_user_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_i_user_roles",
                table: "i_user_roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_i_user_logins",
                table: "i_user_logins");

            migrationBuilder.DropPrimaryKey(
                name: "pk_i_user_claims",
                table: "i_user_claims");

            migrationBuilder.DropPrimaryKey(
                name: "pk_i_roles",
                table: "i_roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_i_role_claims",
                table: "i_role_claims");

            migrationBuilder.RenameTable(
                name: "i_users",
                newName: "__users");

            migrationBuilder.RenameTable(
                name: "i_user_tokens",
                newName: "__user_tokens");

            migrationBuilder.RenameTable(
                name: "i_user_roles",
                newName: "__user_roles");

            migrationBuilder.RenameTable(
                name: "i_user_logins",
                newName: "__user_logins");

            migrationBuilder.RenameTable(
                name: "i_user_claims",
                newName: "__user_claims");

            migrationBuilder.RenameTable(
                name: "i_roles",
                newName: "__roles");

            migrationBuilder.RenameTable(
                name: "i_role_claims",
                newName: "__role_claims");

            migrationBuilder.RenameIndex(
                name: "ix_i_user_roles_role_id",
                table: "__user_roles",
                newName: "ix___user_roles_role_id");

            migrationBuilder.RenameIndex(
                name: "ix_i_user_logins_user_id",
                table: "__user_logins",
                newName: "ix___user_logins_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_i_user_claims_user_id",
                table: "__user_claims",
                newName: "ix___user_claims_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_i_role_claims_role_id",
                table: "__role_claims",
                newName: "ix___role_claims_role_id");

            migrationBuilder.AddColumn<string>(
                name: "app_user_id",
                table: "blogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk___users",
                table: "__users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk___user_tokens",
                table: "__user_tokens",
                columns: new[] { "user_id", "login_provider", "name" });

            migrationBuilder.AddPrimaryKey(
                name: "pk___user_roles",
                table: "__user_roles",
                columns: new[] { "user_id", "role_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk___user_logins",
                table: "__user_logins",
                columns: new[] { "login_provider", "provider_key" });

            migrationBuilder.AddPrimaryKey(
                name: "pk___user_claims",
                table: "__user_claims",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk___roles",
                table: "__roles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk___role_claims",
                table: "__role_claims",
                column: "id");

            migrationBuilder.CreateTable(
                name: "hashtags",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    usage_count = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hashtags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staffs",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staffs", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_staffs___users_user_id",
                        column: x => x.user_id,
                        principalTable: "__users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tutors",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    verification_status = table.Column<int>(type: "integer", nullable: false),
                    last_status_update_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tutors", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_tutors___users_user_id",
                        column: x => x.user_id,
                        principalTable: "__users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tutor_applications",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tutor_id = table.Column<string>(type: "text", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    revision_notes = table.Column<string>(type: "text", nullable: false),
                    internal_notes = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tutor_applications", x => x.id);
                    table.ForeignKey(
                        name: "fk_tutor_applications__tutors_tutor_temp_id",
                        column: x => x.tutor_id,
                        principalTable: "tutors",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tutor_hashtags",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tutor_id = table.Column<string>(type: "text", nullable: false),
                    hashtag_id = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tutor_hashtags", x => x.id);
                    table.ForeignKey(
                        name: "fk_tutor_hashtags__tutors_tutor_temp_id1",
                        column: x => x.tutor_id,
                        principalTable: "tutors",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tutor_hashtags_hashtags_hashtag_id",
                        column: x => x.hashtag_id,
                        principalTable: "hashtags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tutor_languages",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tutor_id = table.Column<string>(type: "text", nullable: false),
                    language_code = table.Column<string>(type: "text", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    proficiency = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tutor_languages", x => x.id);
                    table.ForeignKey(
                        name: "fk_tutor_languages__tutors_tutor_temp_id2",
                        column: x => x.tutor_id,
                        principalTable: "tutors",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "application_revisions",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    application_id = table.Column<string>(type: "text", nullable: false),
                    staff_id = table.Column<string>(type: "text", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_application_revisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_application_revisions__staffs_staff_temp_id",
                        column: x => x.staff_id,
                        principalTable: "staffs",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_application_revisions__tutor_applications_application_id",
                        column: x => x.application_id,
                        principalTable: "tutor_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    application_id = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    staff_id = table.Column<string>(type: "text", nullable: true),
                    is_visible_to_learner = table.Column<bool>(type: "boolean", nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    storage_path = table.Column<string>(type: "text", nullable: false),
                    download_url = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_documents__staffs_staff_temp_id1",
                        column: x => x.staff_id,
                        principalTable: "staffs",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_documents__tutor_applications_application_id",
                        column: x => x.application_id,
                        principalTable: "tutor_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_blogs_app_user_id",
                table: "blogs",
                column: "app_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_revisions_application_id",
                table: "application_revisions",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "IX_application_revisions_staff_id",
                table: "application_revisions",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "ix_documents_application_id",
                table: "documents",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_staff_id",
                table: "documents",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_tutor_applications_tutor_id",
                table: "tutor_applications",
                column: "tutor_id");

            migrationBuilder.CreateIndex(
                name: "ix_tutor_hashtags_hashtag_id",
                table: "tutor_hashtags",
                column: "hashtag_id");

            migrationBuilder.CreateIndex(
                name: "IX_tutor_hashtags_tutor_id",
                table: "tutor_hashtags",
                column: "tutor_id");

            migrationBuilder.CreateIndex(
                name: "IX_tutor_languages_tutor_id",
                table: "tutor_languages",
                column: "tutor_id");

            migrationBuilder.AddForeignKey(
                name: "fk___role_claims___roles_role_id",
                table: "__role_claims",
                column: "role_id",
                principalTable: "__roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk___user_claims___users_user_id",
                table: "__user_claims",
                column: "user_id",
                principalTable: "__users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk___user_logins___users_user_id",
                table: "__user_logins",
                column: "user_id",
                principalTable: "__users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk___user_roles___roles_role_id",
                table: "__user_roles",
                column: "role_id",
                principalTable: "__roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk___user_roles___users_user_id",
                table: "__user_roles",
                column: "user_id",
                principalTable: "__users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk___user_tokens___users_user_id",
                table: "__user_tokens",
                column: "user_id",
                principalTable: "__users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_blogs___users_app_user_id",
                table: "blogs",
                column: "app_user_id",
                principalTable: "__users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk___role_claims___roles_role_id",
                table: "__role_claims");

            migrationBuilder.DropForeignKey(
                name: "fk___user_claims___users_user_id",
                table: "__user_claims");

            migrationBuilder.DropForeignKey(
                name: "fk___user_logins___users_user_id",
                table: "__user_logins");

            migrationBuilder.DropForeignKey(
                name: "fk___user_roles___roles_role_id",
                table: "__user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk___user_roles___users_user_id",
                table: "__user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk___user_tokens___users_user_id",
                table: "__user_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_blogs___users_app_user_id",
                table: "blogs");

            migrationBuilder.DropTable(
                name: "application_revisions");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "tutor_hashtags");

            migrationBuilder.DropTable(
                name: "tutor_languages");

            migrationBuilder.DropTable(
                name: "staffs");

            migrationBuilder.DropTable(
                name: "tutor_applications");

            migrationBuilder.DropTable(
                name: "hashtags");

            migrationBuilder.DropTable(
                name: "tutors");

            migrationBuilder.DropIndex(
                name: "ix_blogs_app_user_id",
                table: "blogs");

            migrationBuilder.DropPrimaryKey(
                name: "pk___users",
                table: "__users");

            migrationBuilder.DropPrimaryKey(
                name: "pk___user_tokens",
                table: "__user_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk___user_roles",
                table: "__user_roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk___user_logins",
                table: "__user_logins");

            migrationBuilder.DropPrimaryKey(
                name: "pk___user_claims",
                table: "__user_claims");

            migrationBuilder.DropPrimaryKey(
                name: "pk___roles",
                table: "__roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk___role_claims",
                table: "__role_claims");

            migrationBuilder.DropColumn(
                name: "app_user_id",
                table: "blogs");

            migrationBuilder.RenameTable(
                name: "__users",
                newName: "i_users");

            migrationBuilder.RenameTable(
                name: "__user_tokens",
                newName: "i_user_tokens");

            migrationBuilder.RenameTable(
                name: "__user_roles",
                newName: "i_user_roles");

            migrationBuilder.RenameTable(
                name: "__user_logins",
                newName: "i_user_logins");

            migrationBuilder.RenameTable(
                name: "__user_claims",
                newName: "i_user_claims");

            migrationBuilder.RenameTable(
                name: "__roles",
                newName: "i_roles");

            migrationBuilder.RenameTable(
                name: "__role_claims",
                newName: "i_role_claims");

            migrationBuilder.RenameIndex(
                name: "ix___user_roles_role_id",
                table: "i_user_roles",
                newName: "ix_i_user_roles_role_id");

            migrationBuilder.RenameIndex(
                name: "ix___user_logins_user_id",
                table: "i_user_logins",
                newName: "ix_i_user_logins_user_id");

            migrationBuilder.RenameIndex(
                name: "ix___user_claims_user_id",
                table: "i_user_claims",
                newName: "ix_i_user_claims_user_id");

            migrationBuilder.RenameIndex(
                name: "ix___role_claims_role_id",
                table: "i_role_claims",
                newName: "ix_i_role_claims_role_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_i_users",
                table: "i_users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_i_user_tokens",
                table: "i_user_tokens",
                columns: new[] { "user_id", "login_provider", "name" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_i_user_roles",
                table: "i_user_roles",
                columns: new[] { "user_id", "role_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_i_user_logins",
                table: "i_user_logins",
                columns: new[] { "login_provider", "provider_key" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_i_user_claims",
                table: "i_user_claims",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_i_roles",
                table: "i_roles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_i_role_claims",
                table: "i_role_claims",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_blogs_user_id",
                table: "blogs",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_blogs_i_users_app_user_id",
                table: "blogs",
                column: "user_id",
                principalTable: "i_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_i_role_claims_i_roles_role_id",
                table: "i_role_claims",
                column: "role_id",
                principalTable: "i_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_i_user_claims_i_users_user_id",
                table: "i_user_claims",
                column: "user_id",
                principalTable: "i_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_i_user_logins_i_users_user_id",
                table: "i_user_logins",
                column: "user_id",
                principalTable: "i_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_i_user_roles_i_roles_role_id",
                table: "i_user_roles",
                column: "role_id",
                principalTable: "i_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_i_user_roles_i_users_user_id",
                table: "i_user_roles",
                column: "user_id",
                principalTable: "i_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_i_user_tokens_i_users_user_id",
                table: "i_user_tokens",
                column: "user_id",
                principalTable: "i_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
