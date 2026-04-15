using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class allmodelsv25 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemReviews_Items_ItemId",
                table: "ItemReviews");

            migrationBuilder.DropIndex(
                name: "IX_SupportThreads_Subject",
                table: "SupportThreads");

            migrationBuilder.DropIndex(
                name: "IX_Reports_Reasons",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Items_Slug",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Fines_PaymentMethod",
                table: "Fines");

            migrationBuilder.DropIndex(
                name: "IX_DirectMessages_ReadAt",
                table: "DirectMessages");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PhoneNumberVerifiedAt",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_UnpaidFinesTotal",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Appeals_AppealType",
                table: "Appeals");

            migrationBuilder.DropIndex(
                name: "IX_Appeals_Status_AppealType",
                table: "Appeals");

            migrationBuilder.DropColumn(
                name: "UnpaidFinesTotal",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DisputeId",
                table: "ScoreHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisputeDeadline",
                table: "Loans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoteToOwner",
                table: "Loans",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerDaySnapshot",
                table: "Loans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Items",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Items",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "Items",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ItemReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByAdminId",
                table: "ItemReviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ItemReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidAt",
                table: "Fines",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstViewedByOtherPartyAt",
                table: "Disputes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsViewedByOtherParty",
                table: "Disputes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastScoreAppealRejectedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Appeals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Appeals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_UserId_Status",
                table: "VerificationRequests",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_IsDeleted",
                table: "UserReviews",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreHistories_DisputeId",
                table: "ScoreHistories",
                column: "DisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_DisputeDeadline",
                table: "Loans",
                column: "DisputeDeadline");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_Status_DisputeDeadline",
                table: "Loans",
                columns: new[] { "Status", "DisputeDeadline" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_DeletedByUserId",
                table: "Items",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Slug",
                table: "Items",
                column: "Slug",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Item_Price_NonNegative",
                table: "Items",
                sql: "[PricePerDay] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_LoanId_DisputeId_Status",
                table: "Fines",
                columns: new[] { "LoanId", "DisputeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_LoanId_FiledAs",
                table: "Disputes",
                columns: new[] { "LoanId", "FiledAs" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LastScoreAppealRejectedAt",
                table: "AspNetUsers",
                column: "LastScoreAppealRejectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_AppealType_Status",
                table: "Appeals",
                columns: new[] { "AppealType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_UserId_AppealType_Status",
                table: "Appeals",
                columns: new[] { "UserId", "AppealType", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_ItemReviews_Items_ItemId",
                table: "ItemReviews",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_AspNetUsers_DeletedByUserId",
                table: "Items",
                column: "DeletedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreHistories_Disputes_DisputeId",
                table: "ScoreHistories",
                column: "DisputeId",
                principalTable: "Disputes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemReviews_Items_ItemId",
                table: "ItemReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_AspNetUsers_DeletedByUserId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_ScoreHistories_Disputes_DisputeId",
                table: "ScoreHistories");

            migrationBuilder.DropIndex(
                name: "IX_VerificationRequests_UserId_Status",
                table: "VerificationRequests");

            migrationBuilder.DropIndex(
                name: "IX_UserReviews_IsDeleted",
                table: "UserReviews");

            migrationBuilder.DropIndex(
                name: "IX_ScoreHistories_DisputeId",
                table: "ScoreHistories");

            migrationBuilder.DropIndex(
                name: "IX_Loans_DisputeDeadline",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_Status_DisputeDeadline",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Items_DeletedByUserId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Slug",
                table: "Items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Item_Price_NonNegative",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Fines_LoanId_DisputeId_Status",
                table: "Fines");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_LoanId_FiledAs",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LastScoreAppealRejectedAt",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Appeals_AppealType_Status",
                table: "Appeals");

            migrationBuilder.DropIndex(
                name: "IX_Appeals_UserId_AppealType_Status",
                table: "Appeals");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserReviews");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserReviews");

            migrationBuilder.DropColumn(
                name: "DisputeId",
                table: "ScoreHistories");

            migrationBuilder.DropColumn(
                name: "DisputeDeadline",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "NoteToOwner",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "PricePerDaySnapshot",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ItemReviews");

            migrationBuilder.DropColumn(
                name: "DeletedByAdminId",
                table: "ItemReviews");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ItemReviews");

            migrationBuilder.DropColumn(
                name: "VoidAt",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "FirstViewedByOtherPartyAt",
                table: "Disputes");

            migrationBuilder.DropColumn(
                name: "IsViewedByOtherParty",
                table: "Disputes");

            migrationBuilder.DropColumn(
                name: "LastScoreAppealRejectedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Appeals");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Appeals");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Items",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<decimal>(
                name: "UnpaidFinesTotal",
                table: "AspNetUsers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_SupportThreads_Subject",
                table: "SupportThreads",
                column: "Subject");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Reasons",
                table: "Reports",
                column: "Reasons");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Slug",
                table: "Items",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_PaymentMethod",
                table: "Fines",
                column: "PaymentMethod");

            migrationBuilder.CreateIndex(
                name: "IX_DirectMessages_ReadAt",
                table: "DirectMessages",
                column: "ReadAt");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PhoneNumberVerifiedAt",
                table: "AspNetUsers",
                column: "PhoneNumberVerifiedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UnpaidFinesTotal",
                table: "AspNetUsers",
                column: "UnpaidFinesTotal");

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_AppealType",
                table: "Appeals",
                column: "AppealType");

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_Status_AppealType",
                table: "Appeals",
                columns: new[] { "Status", "AppealType" });

            migrationBuilder.AddForeignKey(
                name: "FK_ItemReviews_Items_ItemId",
                table: "ItemReviews",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
