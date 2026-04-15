using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "SupportThreads",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LenderId",
                table: "Loans",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "DirectMessages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "DirectMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeletedForReceiver",
                table: "DirectMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeletedForSender",
                table: "DirectMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "DirectMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMessageAt",
                table: "DirectConversations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastMessageId",
                table: "DirectConversations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MessageCount",
                table: "DirectConversations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SupportThreads_Subject",
                table: "SupportThreads",
                column: "Subject");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_LenderId",
                table: "Loans",
                column: "LenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_OwnerId_IsActive_Status",
                table: "Items",
                columns: new[] { "OwnerId", "IsActive", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DirectMessages_ReadAt",
                table: "DirectMessages",
                column: "ReadAt");

            migrationBuilder.CreateIndex(
                name: "IX_DirectConversations_LastMessageAt",
                table: "DirectConversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_DirectConversations_LastMessageId",
                table: "DirectConversations",
                column: "LastMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_DirectConversations_DirectMessages_LastMessageId",
                table: "DirectConversations",
                column: "LastMessageId",
                principalTable: "DirectMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Loans_AspNetUsers_LenderId",
                table: "Loans",
                column: "LenderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DirectConversations_DirectMessages_LastMessageId",
                table: "DirectConversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Loans_AspNetUsers_LenderId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_SupportThreads_Subject",
                table: "SupportThreads");

            migrationBuilder.DropIndex(
                name: "IX_Loans_LenderId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Items_OwnerId_IsActive_Status",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_DirectMessages_ReadAt",
                table: "DirectMessages");

            migrationBuilder.DropIndex(
                name: "IX_DirectConversations_LastMessageAt",
                table: "DirectConversations");

            migrationBuilder.DropIndex(
                name: "IX_DirectConversations_LastMessageId",
                table: "DirectConversations");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "SupportThreads");

            migrationBuilder.DropColumn(
                name: "LenderId",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "DirectMessages");

            migrationBuilder.DropColumn(
                name: "IsDeletedForReceiver",
                table: "DirectMessages");

            migrationBuilder.DropColumn(
                name: "IsDeletedForSender",
                table: "DirectMessages");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "DirectMessages");

            migrationBuilder.DropColumn(
                name: "LastMessageAt",
                table: "DirectConversations");

            migrationBuilder.DropColumn(
                name: "LastMessageId",
                table: "DirectConversations");

            migrationBuilder.DropColumn(
                name: "MessageCount",
                table: "DirectConversations");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "DirectMessages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);
        }
    }
}
