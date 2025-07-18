using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletAndTransTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bank_accounts",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    bank_name = table.Column<string>(type: "text", nullable: false),
                    account_number = table.Column<string>(type: "text", nullable: false),
                    account_holder_name = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_bank_accounts___users_user_id",
                        column: x => x.user_id,
                        principalTable: "__users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deposit_requests",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    payment_gateway = table.Column<string>(type: "text", nullable: false, defaultValue: "PayOS"),
                    gateway_transaction_id = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payos_order_url = table.Column<string>(type: "text", nullable: true),
                    payos_order_token = table.Column<string>(type: "text", nullable: true),
                    payos_qr_code = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deposit_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_deposit_requests___users_user_id",
                        column: x => x.user_id,
                        principalTable: "__users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fee_configs",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    fee_code = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fee_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "held_funds",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    booked_slot_id = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    release_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_held_funds", x => x.id);
                    table.ForeignKey(
                        name: "fk_held_funds__booked_slots_booked_slot_id",
                        column: x => x.booked_slot_id,
                        principalTable: "booked_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallets", x => x.id);
                    table.ForeignKey(
                        name: "fk_wallets___users_user_id",
                        column: x => x.user_id,
                        principalTable: "__users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "withdrawal_requests",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    bank_account_id = table.Column<string>(type: "text", nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    fees = table.Column<string>(type: "text", nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_withdrawal_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_withdrawal_requests___users_user_id",
                        column: x => x.user_id,
                        principalTable: "__users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withdrawal_requests_bank_accounts_bank_account_id",
                        column: x => x.bank_account_id,
                        principalTable: "bank_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    source_wallet_id = table.Column<string>(type: "text", nullable: true),
                    target_wallet_id = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    reference_id = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_transactions_wallets_source_wallet_id",
                        column: x => x.source_wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_wallets_target_wallet_id",
                        column: x => x.target_wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bank_accounts_user_id",
                table: "bank_accounts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_deposit_requests_user_id",
                table: "deposit_requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_held_funds_booked_slot_id",
                table: "held_funds",
                column: "booked_slot_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_source_wallet_id",
                table: "transactions",
                column: "source_wallet_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_target_wallet_id",
                table: "transactions",
                column: "target_wallet_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallets_user_id",
                table: "wallets",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_requests_bank_account_id",
                table: "withdrawal_requests",
                column: "bank_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_requests_user_id",
                table: "withdrawal_requests",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deposit_requests");

            migrationBuilder.DropTable(
                name: "fee_configs");

            migrationBuilder.DropTable(
                name: "held_funds");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "withdrawal_requests");

            migrationBuilder.DropTable(
                name: "wallets");

            migrationBuilder.DropTable(
                name: "bank_accounts");
        }
    }
}
