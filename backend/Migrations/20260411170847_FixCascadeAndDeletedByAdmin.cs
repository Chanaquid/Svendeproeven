using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeAndDeletedByAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DirectConversations_DirectMessages_LastMessageId",
                table: "DirectConversations");

            migrationBuilder.AddForeignKey(
                name: "FK_DirectConversations_DirectMessages_LastMessageId",
                table: "DirectConversations",
                column: "LastMessageId",
                principalTable: "DirectMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DirectConversations_DirectMessages_LastMessageId",
                table: "DirectConversations");

            migrationBuilder.AddForeignKey(
                name: "FK_DirectConversations_DirectMessages_LastMessageId",
                table: "DirectConversations",
                column: "LastMessageId",
                principalTable: "DirectMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
