using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class updatedv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appeals_Fines_FineId",
                table: "Appeals");

            migrationBuilder.DropForeignKey(
                name: "FK_Appeals_ScoreHistories_ScoreHistoryId",
                table: "Appeals");

            migrationBuilder.DropForeignKey(
                name: "FK_Fines_Disputes_DisputeId",
                table: "Fines");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemReviews_Items_ItemId",
                table: "ItemReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_ScoreHistories_AspNetUsers_UserId",
                table: "ScoreHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_ScoreHistories_Loans_LoanId",
                table: "ScoreHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_VerificationRequests_AspNetUsers_UserId",
                table: "VerificationRequests");

            migrationBuilder.DropTable(
                name: "ReportReasonEntries");

            migrationBuilder.DropIndex(
                name: "IX_VerificationRequests_UserId_Status",
                table: "VerificationRequests");

            migrationBuilder.DropIndex(
                name: "IX_UserReviews_LoanId_ReviewerId",
                table: "UserReviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserReview_Rating",
                table: "UserReviews");

            migrationBuilder.DropIndex(
                name: "IX_SupportThreads_UserId_Status",
                table: "SupportThreads");

            migrationBuilder.DropIndex(
                name: "IX_SupportMessages_SupportThreadId_SentAt",
                table: "SupportMessages");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReportedById_CreatedAt",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Loans_Status_EndDate",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_LoanMessages_LoanId_SentAt",
                table: "LoanMessages");

            migrationBuilder.DropIndex(
                name: "IX_Items_Status_IsActive",
                table: "Items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ItemReview_Rating",
                table: "ItemReviews");

            migrationBuilder.DropIndex(
                name: "IX_DirectMessages_ConversationId_SentAt",
                table: "DirectMessages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_User_Score",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Appeals_FineId",
                table: "Appeals");

            migrationBuilder.DropColumn(
                name: "ClaimedAt",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ClaimedByAdminId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ResolvedByAdminId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "DecidedById",
                table: "Loans");

            migrationBuilder.RenameColumn(
                name: "ReviewedAt",
                table: "Loans",
                newName: "OwnerApprovedAt");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "VerificationRequests",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "DocumentUrl",
                table: "VerificationRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<int>(
                name: "DocumentType",
                table: "VerificationRequests",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AdminNote",
                table: "VerificationRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "UserReviews",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "SupportThreads",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ClaimedByAdminId",
                table: "SupportThreads",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "SupportMessages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<int>(
                name: "Reason",
                table: "ScoreHistories",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "ScoreHistories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Reports",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Reports",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "AdminNote",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalDetails",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandledByAdminId",
                table: "Reports",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Reasons",
                table: "Reports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Notifications",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "ReferenceType",
                table: "Notifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "PhotoUrl",
                table: "LoanSnapshotPhotos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalPrice",
                table: "Loans",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Loans",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<int>(
                name: "SnapshotCondition",
                table: "Loans",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "ExtensionRequestStatus",
                table: "Loans",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DecisionNote",
                table: "Loans",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AdminReviewedAt",
                table: "Loans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminReviewerId",
                table: "Loans",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerApproverId",
                table: "Loans",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "LoanMessages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Items",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewedByAdminId",
                table: "Items",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerDay",
                table: "Items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentValue",
                table: "Items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<int>(
                name: "Condition",
                table: "Items",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "Availability",
                table: "Items",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AdminNote",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Items",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "ItemReviews",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhotoUrl",
                table: "ItemPhotos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "VerifiedByAdminId",
                table: "Fines",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Fines",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Fines",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "Fines",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentProofImageUrl",
                table: "Fines",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PaymentMethod",
                table: "Fines",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentDescription",
                table: "Fines",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ItemValueAtTimeOfFine",
                table: "Fines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<string>(
                name: "IssuedByAdminId",
                table: "Fines",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Fines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<string>(
                name: "AdminNote",
                table: "Fines",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Disputes",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ResponseDescription",
                table: "Disputes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RespondedById",
                table: "Disputes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResolvedByAdminId",
                table: "Disputes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FiledAs",
                table: "Disputes",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Disputes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<decimal>(
                name: "CustomFineAmount",
                table: "Disputes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AdminVerdict",
                table: "Disputes",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdminNote",
                table: "Disputes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhotoUrl",
                table: "DisputePhotos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "Caption",
                table: "DisputePhotos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "DirectMessages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnpaidFinesTotal",
                table: "AspNetUsers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 100);

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BannedByAdminId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BanExpiresAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Appeals",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ResolvedByAdminId",
                table: "Appeals",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Appeals",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<int>(
                name: "FineResolution",
                table: "Appeals",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CustomFineAmount",
                table: "Appeals",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AppealType",
                table: "Appeals",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AdminNote",
                table: "Appeals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "UserBanHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AdminId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsBanned = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BannedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BanExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBanHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBanHistories_AspNetUsers_AdminId",
                        column: x => x.AdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBanHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_DocumentType",
                table: "VerificationRequests",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_ReviewedByAdminId",
                table: "VerificationRequests",
                column: "ReviewedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_Status_SubmittedAt",
                table: "VerificationRequests",
                columns: new[] { "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_SubmittedAt",
                table: "VerificationRequests",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_UserId",
                table: "VerificationRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_CreatedAt",
                table: "UserReviews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_IsAdminReview",
                table: "UserReviews",
                column: "IsAdminReview");

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_LoanId",
                table: "UserReviews",
                column: "LoanId",
                unique: true,
                filter: "[LoanId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_ReviewedUserId_Rating",
                table: "UserReviews",
                columns: new[] { "ReviewedUserId", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_ReviewerId_ReviewedUserId",
                table: "UserReviews",
                columns: new[] { "ReviewerId", "ReviewedUserId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserReview_NoSelfReview",
                table: "UserReviews",
                sql: "[ReviewerId] != [ReviewedUserId]");

            migrationBuilder.CreateIndex(
                name: "IX_UserRecentlyViewedItems_UserId",
                table: "UserRecentlyViewedItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteItems_SavedAt",
                table: "UserFavoriteItems",
                column: "SavedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteItems_UserId",
                table: "UserFavoriteItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteItems_UserId_NotifyWhenAvailable",
                table: "UserFavoriteItems",
                columns: new[] { "UserId", "NotifyWhenAvailable" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockerId",
                table: "UserBlocks",
                column: "BlockerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_CreatedAt",
                table: "UserBlocks",
                column: "CreatedAt");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserBlock_NoSelfBlock",
                table: "UserBlocks",
                sql: "[BlockerId] != [BlockedId]");

            migrationBuilder.CreateIndex(
                name: "IX_SupportThreads_ClaimedByAdminId",
                table: "SupportThreads",
                column: "ClaimedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportThreads_CreatedAt",
                table: "SupportThreads",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportThreads_Status_ClaimedByAdminId",
                table: "SupportThreads",
                columns: new[] { "Status", "ClaimedByAdminId" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportThreads_UserId",
                table: "SupportThreads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_SentAt",
                table: "SupportMessages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_SupportThreadId",
                table: "SupportMessages",
                column: "SupportThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_SupportThreadId_IsRead",
                table: "SupportMessages",
                columns: new[] { "SupportThreadId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_ScoreHistories_CreatedAt",
                table: "ScoreHistories",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreHistories_Reason",
                table: "ScoreHistories",
                column: "Reason");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreHistories_UserId",
                table: "ScoreHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_CreatedAt",
                table: "Reports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_HandledByAdminId",
                table: "Reports",
                column: "HandledByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Reasons",
                table: "Reports",
                column: "Reasons");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedById",
                table: "Reports",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Status_Type",
                table: "Reports",
                columns: new[] { "Status", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_TargetId",
                table: "Reports",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Type",
                table: "Reports",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Type_TargetId",
                table: "Reports",
                columns: new[] { "Type", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReferenceType_ReferenceId",
                table: "Notifications",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                table: "Notifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanSnapshotPhotos_LoanId_DisplayOrder",
                table: "LoanSnapshotPhotos",
                columns: new[] { "LoanId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_AdminReviewerId",
                table: "Loans",
                column: "AdminReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_CreatedAt",
                table: "Loans",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_EndDate_Status",
                table: "Loans",
                columns: new[] { "EndDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_ExtensionRequestStatus",
                table: "Loans",
                column: "ExtensionRequestStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_OwnerApproverId",
                table: "Loans",
                column: "OwnerApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_PickedUpAt",
                table: "Loans",
                column: "PickedUpAt");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_ReturnedAt",
                table: "Loans",
                column: "ReturnedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_StartDate_EndDate",
                table: "Loans",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_Status_BorrowerId",
                table: "Loans",
                columns: new[] { "Status", "BorrowerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_Status_ItemId",
                table: "Loans",
                columns: new[] { "Status", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanMessages_LoanId",
                table: "LoanMessages",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanMessages_LoanId_IsRead",
                table: "LoanMessages",
                columns: new[] { "LoanId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanMessages_SentAt",
                table: "LoanMessages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Availability",
                table: "Items",
                column: "Availability");

            migrationBuilder.CreateIndex(
                name: "IX_Items_AvailableFrom_AvailableUntil",
                table: "Items",
                columns: new[] { "AvailableFrom", "AvailableUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_CreatedAt",
                table: "Items",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Items_IsActive",
                table: "Items",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Items_IsFree",
                table: "Items",
                column: "IsFree");

            migrationBuilder.CreateIndex(
                name: "IX_Items_RequiresVerification",
                table: "Items",
                column: "RequiresVerification");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ReviewedByAdminId",
                table: "Items",
                column: "ReviewedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Slug",
                table: "Items",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Status",
                table: "Items",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Status_Availability_IsActive",
                table: "Items",
                columns: new[] { "Status", "Availability", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_Status_IsActive_CategoryId",
                table: "Items",
                columns: new[] { "Status", "IsActive", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemReviews_CreatedAt",
                table: "ItemReviews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ItemReviews_IsAdminReview",
                table: "ItemReviews",
                column: "IsAdminReview");

            migrationBuilder.CreateIndex(
                name: "IX_ItemReviews_ItemId_Rating",
                table: "ItemReviews",
                columns: new[] { "ItemId", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemPhotos_ItemId_DisplayOrder",
                table: "ItemPhotos",
                columns: new[] { "ItemId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemPhotos_ItemId_IsPrimary",
                table: "ItemPhotos",
                columns: new[] { "ItemId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_Fines_CreatedAt",
                table: "Fines",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_IssuedByAdminId",
                table: "Fines",
                column: "IssuedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_PaymentMethod",
                table: "Fines",
                column: "PaymentMethod");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_Status",
                table: "Fines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_Type",
                table: "Fines",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_UserId",
                table: "Fines",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_VerifiedByAdminId",
                table: "Fines",
                column: "VerifiedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_CreatedAt",
                table: "Disputes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_FiledAs",
                table: "Disputes",
                column: "FiledAs");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_ResolvedByAdminId",
                table: "Disputes",
                column: "ResolvedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_RespondedById",
                table: "Disputes",
                column: "RespondedById");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_Status",
                table: "Disputes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_Status_ResponseDeadline",
                table: "Disputes",
                columns: new[] { "Status", "ResponseDeadline" });

            migrationBuilder.CreateIndex(
                name: "IX_DisputePhotos_UploadedAt",
                table: "DisputePhotos",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DirectMessages_ConversationId",
                table: "DirectMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectMessages_ConversationId_IsRead",
                table: "DirectMessages",
                columns: new[] { "ConversationId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_DirectMessages_SentAt",
                table: "DirectMessages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_DirectConversations_CreatedAt",
                table: "DirectConversations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DirectConversations_InitiatedById",
                table: "DirectConversations",
                column: "InitiatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DirectConversations_InitiatedById_HiddenForInitiator",
                table: "DirectConversations",
                columns: new[] { "InitiatedById", "HiddenForInitiator" });

            migrationBuilder.CreateIndex(
                name: "IX_DirectConversations_OtherUserId_HiddenForOther",
                table: "DirectConversations",
                columns: new[] { "OtherUserId", "HiddenForOther" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_DirectConversation_DifferentUsers",
                table: "DirectConversations",
                sql: "[InitiatedById] != [OtherUserId]");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive",
                table: "Categories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive_Name",
                table: "Categories",
                columns: new[] { "IsActive", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_BannedByAdminId",
                table: "AspNetUsers",
                column: "BannedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CreatedAt",
                table: "AspNetUsers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DeletedByAdminId",
                table: "AspNetUsers",
                column: "DeletedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IsBanned",
                table: "AspNetUsers",
                column: "IsBanned");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IsDeleted",
                table: "AspNetUsers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IsDeleted_IsBanned",
                table: "AspNetUsers",
                columns: new[] { "IsDeleted", "IsBanned" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IsVerified",
                table: "AspNetUsers",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Latitude_Longitude",
                table: "AspNetUsers",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PhoneNumberVerifiedAt",
                table: "AspNetUsers",
                column: "PhoneNumberVerifiedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Score",
                table: "AspNetUsers",
                column: "Score");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UnpaidFinesTotal",
                table: "AspNetUsers",
                column: "UnpaidFinesTotal");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UserName",
                table: "AspNetUsers",
                column: "UserName",
                unique: true,
                filter: "[UserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_AppealType",
                table: "Appeals",
                column: "AppealType");

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_CreatedAt",
                table: "Appeals",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_FineId",
                table: "Appeals",
                column: "FineId",
                unique: true,
                filter: "[FineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_ResolvedByAdminId",
                table: "Appeals",
                column: "ResolvedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_Status_AppealType",
                table: "Appeals",
                columns: new[] { "Status", "AppealType" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBanHistories_AdminId",
                table: "UserBanHistories",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBanHistories_BanExpiresAt",
                table: "UserBanHistories",
                column: "BanExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserBanHistories_BannedAt",
                table: "UserBanHistories",
                column: "BannedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserBanHistories_IsBanned",
                table: "UserBanHistories",
                column: "IsBanned");

            migrationBuilder.CreateIndex(
                name: "IX_UserBanHistories_UserId",
                table: "UserBanHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBanHistories_UserId_BannedAt",
                table: "UserBanHistories",
                columns: new[] { "UserId", "BannedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Appeals_AspNetUsers_ResolvedByAdminId",
                table: "Appeals",
                column: "ResolvedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appeals_Fines_FineId",
                table: "Appeals",
                column: "FineId",
                principalTable: "Fines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appeals_ScoreHistories_ScoreHistoryId",
                table: "Appeals",
                column: "ScoreHistoryId",
                principalTable: "ScoreHistories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_BannedByAdminId",
                table: "AspNetUsers",
                column: "BannedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_DeletedByAdminId",
                table: "AspNetUsers",
                column: "DeletedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Disputes_AspNetUsers_ResolvedByAdminId",
                table: "Disputes",
                column: "ResolvedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Disputes_AspNetUsers_RespondedById",
                table: "Disputes",
                column: "RespondedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Fines_AspNetUsers_IssuedByAdminId",
                table: "Fines",
                column: "IssuedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Fines_AspNetUsers_VerifiedByAdminId",
                table: "Fines",
                column: "VerifiedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Fines_Disputes_DisputeId",
                table: "Fines",
                column: "DisputeId",
                principalTable: "Disputes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemReviews_Items_ItemId",
                table: "ItemReviews",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_AspNetUsers_ReviewedByAdminId",
                table: "Items",
                column: "ReviewedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Loans_AspNetUsers_AdminReviewerId",
                table: "Loans",
                column: "AdminReviewerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Loans_AspNetUsers_OwnerApproverId",
                table: "Loans",
                column: "OwnerApproverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_HandledByAdminId",
                table: "Reports",
                column: "HandledByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreHistories_AspNetUsers_UserId",
                table: "ScoreHistories",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreHistories_Loans_LoanId",
                table: "ScoreHistories",
                column: "LoanId",
                principalTable: "Loans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupportThreads_AspNetUsers_ClaimedByAdminId",
                table: "SupportThreads",
                column: "ClaimedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VerificationRequests_AspNetUsers_ReviewedByAdminId",
                table: "VerificationRequests",
                column: "ReviewedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VerificationRequests_AspNetUsers_UserId",
                table: "VerificationRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appeals_AspNetUsers_ResolvedByAdminId",
                table: "Appeals");

            migrationBuilder.DropForeignKey(
                name: "FK_Appeals_Fines_FineId",
                table: "Appeals");

            migrationBuilder.DropForeignKey(
                name: "FK_Appeals_ScoreHistories_ScoreHistoryId",
                table: "Appeals");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_BannedByAdminId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_DeletedByAdminId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Disputes_AspNetUsers_ResolvedByAdminId",
                table: "Disputes");

            migrationBuilder.DropForeignKey(
                name: "FK_Disputes_AspNetUsers_RespondedById",
                table: "Disputes");

            migrationBuilder.DropForeignKey(
                name: "FK_Fines_AspNetUsers_IssuedByAdminId",
                table: "Fines");

            migrationBuilder.DropForeignKey(
                name: "FK_Fines_AspNetUsers_VerifiedByAdminId",
                table: "Fines");

            migrationBuilder.DropForeignKey(
                name: "FK_Fines_Disputes_DisputeId",
                table: "Fines");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemReviews_Items_ItemId",
                table: "ItemReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_AspNetUsers_ReviewedByAdminId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Loans_AspNetUsers_AdminReviewerId",
                table: "Loans");

            migrationBuilder.DropForeignKey(
                name: "FK_Loans_AspNetUsers_OwnerApproverId",
                table: "Loans");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_HandledByAdminId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_ScoreHistories_AspNetUsers_UserId",
                table: "ScoreHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_ScoreHistories_Loans_LoanId",
                table: "ScoreHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_SupportThreads_AspNetUsers_ClaimedByAdminId",
                table: "SupportThreads");

            migrationBuilder.DropForeignKey(
                name: "FK_VerificationRequests_AspNetUsers_ReviewedByAdminId",
                table: "VerificationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_VerificationRequests_AspNetUsers_UserId",
                table: "VerificationRequests");

            migrationBuilder.DropTable(
                name: "UserBanHistories");

            migrationBuilder.DropIndex(
                name: "IX_VerificationRequests_DocumentType",
                table: "VerificationRequests");

            migrationBuilder.DropIndex(
                name: "IX_VerificationRequests_ReviewedByAdminId",
                table: "VerificationRequests");

            migrationBuilder.DropIndex(
                name: "IX_VerificationRequests_Status_SubmittedAt",
                table: "VerificationRequests");

            migrationBuilder.DropIndex(
                name: "IX_VerificationRequests_SubmittedAt",
                table: "VerificationRequests");

            migrationBuilder.DropIndex(
                name: "IX_VerificationRequests_UserId",
                table: "VerificationRequests");

            migrationBuilder.DropIndex(
                name: "IX_UserReviews_CreatedAt",
                table: "UserReviews");

            migrationBuilder.DropIndex(
                name: "IX_UserReviews_IsAdminReview",
                table: "UserReviews");

            migrationBuilder.DropIndex(
                name: "IX_UserReviews_LoanId",
                table: "UserReviews");

            migrationBuilder.DropIndex(
                name: "IX_UserReviews_ReviewedUserId_Rating",
                table: "UserReviews");

            migrationBuilder.DropIndex(
                name: "IX_UserReviews_ReviewerId_ReviewedUserId",
                table: "UserReviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserReview_NoSelfReview",
                table: "UserReviews");

            migrationBuilder.DropIndex(
                name: "IX_UserRecentlyViewedItems_UserId",
                table: "UserRecentlyViewedItems");

            migrationBuilder.DropIndex(
                name: "IX_UserFavoriteItems_SavedAt",
                table: "UserFavoriteItems");

            migrationBuilder.DropIndex(
                name: "IX_UserFavoriteItems_UserId",
                table: "UserFavoriteItems");

            migrationBuilder.DropIndex(
                name: "IX_UserFavoriteItems_UserId_NotifyWhenAvailable",
                table: "UserFavoriteItems");

            migrationBuilder.DropIndex(
                name: "IX_UserBlocks_BlockerId",
                table: "UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_UserBlocks_CreatedAt",
                table: "UserBlocks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserBlock_NoSelfBlock",
                table: "UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_SupportThreads_ClaimedByAdminId",
                table: "SupportThreads");

            migrationBuilder.DropIndex(
                name: "IX_SupportThreads_CreatedAt",
                table: "SupportThreads");

            migrationBuilder.DropIndex(
                name: "IX_SupportThreads_Status_ClaimedByAdminId",
                table: "SupportThreads");

            migrationBuilder.DropIndex(
                name: "IX_SupportThreads_UserId",
                table: "SupportThreads");

            migrationBuilder.DropIndex(
                name: "IX_SupportMessages_SentAt",
                table: "SupportMessages");

            migrationBuilder.DropIndex(
                name: "IX_SupportMessages_SupportThreadId",
                table: "SupportMessages");

            migrationBuilder.DropIndex(
                name: "IX_SupportMessages_SupportThreadId_IsRead",
                table: "SupportMessages");

            migrationBuilder.DropIndex(
                name: "IX_ScoreHistories_CreatedAt",
                table: "ScoreHistories");

            migrationBuilder.DropIndex(
                name: "IX_ScoreHistories_Reason",
                table: "ScoreHistories");

            migrationBuilder.DropIndex(
                name: "IX_ScoreHistories_UserId",
                table: "ScoreHistories");

            migrationBuilder.DropIndex(
                name: "IX_Reports_CreatedAt",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_HandledByAdminId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_Reasons",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReportedById",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_Status_Type",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_TargetId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_Type",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_Type_TargetId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ReferenceType_ReferenceId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Type",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_LoanSnapshotPhotos_LoanId_DisplayOrder",
                table: "LoanSnapshotPhotos");

            migrationBuilder.DropIndex(
                name: "IX_Loans_AdminReviewerId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_CreatedAt",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_EndDate_Status",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_ExtensionRequestStatus",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_OwnerApproverId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_PickedUpAt",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_ReturnedAt",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_StartDate_EndDate",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_Status_BorrowerId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_Status_ItemId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_LoanMessages_LoanId",
                table: "LoanMessages");

            migrationBuilder.DropIndex(
                name: "IX_LoanMessages_LoanId_IsRead",
                table: "LoanMessages");

            migrationBuilder.DropIndex(
                name: "IX_LoanMessages_SentAt",
                table: "LoanMessages");

            migrationBuilder.DropIndex(
                name: "IX_Items_Availability",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_AvailableFrom_AvailableUntil",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_CreatedAt",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_IsActive",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_IsFree",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_RequiresVerification",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_ReviewedByAdminId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Slug",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Status",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Status_Availability_IsActive",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Status_IsActive_CategoryId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_ItemReviews_CreatedAt",
                table: "ItemReviews");

            migrationBuilder.DropIndex(
                name: "IX_ItemReviews_IsAdminReview",
                table: "ItemReviews");

            migrationBuilder.DropIndex(
                name: "IX_ItemReviews_ItemId_Rating",
                table: "ItemReviews");

            migrationBuilder.DropIndex(
                name: "IX_ItemPhotos_ItemId_DisplayOrder",
                table: "ItemPhotos");

            migrationBuilder.DropIndex(
                name: "IX_ItemPhotos_ItemId_IsPrimary",
                table: "ItemPhotos");

            migrationBuilder.DropIndex(
                name: "IX_Fines_CreatedAt",
                table: "Fines");

            migrationBuilder.DropIndex(
                name: "IX_Fines_IssuedByAdminId",
                table: "Fines");

            migrationBuilder.DropIndex(
                name: "IX_Fines_PaymentMethod",
                table: "Fines");

            migrationBuilder.DropIndex(
                name: "IX_Fines_Status",
                table: "Fines");

            migrationBuilder.DropIndex(
                name: "IX_Fines_Type",
                table: "Fines");

            migrationBuilder.DropIndex(
                name: "IX_Fines_UserId",
                table: "Fines");

            migrationBuilder.DropIndex(
                name: "IX_Fines_VerifiedByAdminId",
                table: "Fines");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_CreatedAt",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_FiledAs",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_ResolvedByAdminId",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_RespondedById",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_Status",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_Status_ResponseDeadline",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_DisputePhotos_UploadedAt",
                table: "DisputePhotos");

            migrationBuilder.DropIndex(
                name: "IX_DirectMessages_ConversationId",
                table: "DirectMessages");

            migrationBuilder.DropIndex(
                name: "IX_DirectMessages_ConversationId_IsRead",
                table: "DirectMessages");

            migrationBuilder.DropIndex(
                name: "IX_DirectMessages_SentAt",
                table: "DirectMessages");

            migrationBuilder.DropIndex(
                name: "IX_DirectConversations_CreatedAt",
                table: "DirectConversations");

            migrationBuilder.DropIndex(
                name: "IX_DirectConversations_InitiatedById",
                table: "DirectConversations");

            migrationBuilder.DropIndex(
                name: "IX_DirectConversations_InitiatedById_HiddenForInitiator",
                table: "DirectConversations");

            migrationBuilder.DropIndex(
                name: "IX_DirectConversations_OtherUserId_HiddenForOther",
                table: "DirectConversations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DirectConversation_DifferentUsers",
                table: "DirectConversations");

            migrationBuilder.DropIndex(
                name: "IX_Categories_IsActive",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_IsActive_Name",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_BannedByAdminId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CreatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DeletedByAdminId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IsBanned",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IsDeleted",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IsDeleted_IsBanned",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IsVerified",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Latitude_Longitude",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PhoneNumberVerifiedAt",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Score",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_UnpaidFinesTotal",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_UserName",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Appeals_AppealType",
                table: "Appeals");

            migrationBuilder.DropIndex(
                name: "IX_Appeals_CreatedAt",
                table: "Appeals");

            migrationBuilder.DropIndex(
                name: "IX_Appeals_FineId",
                table: "Appeals");

            migrationBuilder.DropIndex(
                name: "IX_Appeals_ResolvedByAdminId",
                table: "Appeals");

            migrationBuilder.DropIndex(
                name: "IX_Appeals_Status_AppealType",
                table: "Appeals");

            migrationBuilder.DropColumn(
                name: "HandledByAdminId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Reasons",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "AdminReviewedAt",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "AdminReviewerId",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "OwnerApproverId",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "BanExpiresAt",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "OwnerApprovedAt",
                table: "Loans",
                newName: "ReviewedAt");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "VerificationRequests",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "DocumentUrl",
                table: "VerificationRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DocumentType",
                table: "VerificationRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "AdminNote",
                table: "VerificationRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "UserReviews",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "SupportThreads",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ClaimedByAdminId",
                table: "SupportThreads",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "SupportMessages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "ScoreHistories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "ScoreHistories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Reports",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "AdminNote",
                table: "Reports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalDetails",
                table: "Reports",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedAt",
                table: "Reports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimedByAdminId",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedByAdminId",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceType",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PhotoUrl",
                table: "LoanSnapshotPhotos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalPrice",
                table: "Loans",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Loans",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "SnapshotCondition",
                table: "Loans",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ExtensionRequestStatus",
                table: "Loans",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DecisionNote",
                table: "Loans",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecidedById",
                table: "Loans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "LoanMessages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Items",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewedByAdminId",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerDay",
                table: "Items",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentValue",
                table: "Items",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Condition",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Availability",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "AdminNote",
                table: "Items",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "ItemReviews",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhotoUrl",
                table: "ItemPhotos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "VerifiedByAdminId",
                table: "Fines",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Fines",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Fines",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "Fines",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentProofImageUrl",
                table: "Fines",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Fines",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentDescription",
                table: "Fines",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ItemValueAtTimeOfFine",
                table: "Fines",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IssuedByAdminId",
                table: "Fines",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Fines",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "AdminNote",
                table: "Fines",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Disputes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ResponseDescription",
                table: "Disputes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RespondedById",
                table: "Disputes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResolvedByAdminId",
                table: "Disputes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FiledAs",
                table: "Disputes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Disputes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CustomFineAmount",
                table: "Disputes",
                type: "decimal(10,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdminVerdict",
                table: "Disputes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdminNote",
                table: "Disputes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhotoUrl",
                table: "DisputePhotos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Caption",
                table: "DisputePhotos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "DirectMessages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Categories",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnpaidFinesTotal",
                table: "AspNetUsers",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "AspNetUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BannedByAdminId",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                table: "AspNetUsers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Appeals",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ResolvedByAdminId",
                table: "Appeals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Appeals",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FineResolution",
                table: "Appeals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CustomFineAmount",
                table: "Appeals",
                type: "decimal(10,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AppealType",
                table: "Appeals",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "AdminNote",
                table: "Appeals",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ReportReasonEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportReasonEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportReasonEntries_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_UserId_Status",
                table: "VerificationRequests",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_LoanId_ReviewerId",
                table: "UserReviews",
                columns: new[] { "LoanId", "ReviewerId" },
                unique: true,
                filter: "[LoanId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserReview_Rating",
                table: "UserReviews",
                sql: "[Rating] >= 1 AND [Rating] <= 5");

            migrationBuilder.CreateIndex(
                name: "IX_SupportThreads_UserId_Status",
                table: "SupportThreads",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_SupportThreadId_SentAt",
                table: "SupportMessages",
                columns: new[] { "SupportThreadId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedById_CreatedAt",
                table: "Reports",
                columns: new[] { "ReportedById", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_Status_EndDate",
                table: "Loans",
                columns: new[] { "Status", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanMessages_LoanId_SentAt",
                table: "LoanMessages",
                columns: new[] { "LoanId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_Status_IsActive",
                table: "Items",
                columns: new[] { "Status", "IsActive" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ItemReview_Rating",
                table: "ItemReviews",
                sql: "[Rating] >= 1 AND [Rating] <= 5");

            migrationBuilder.CreateIndex(
                name: "IX_DirectMessages_ConversationId_SentAt",
                table: "DirectMessages",
                columns: new[] { "ConversationId", "SentAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_User_Score",
                table: "AspNetUsers",
                sql: "[Score] >= 0 AND [Score] <= 100");

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_FineId",
                table: "Appeals",
                column: "FineId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportReasonEntries_ReportId",
                table: "ReportReasonEntries",
                column: "ReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appeals_Fines_FineId",
                table: "Appeals",
                column: "FineId",
                principalTable: "Fines",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Appeals_ScoreHistories_ScoreHistoryId",
                table: "Appeals",
                column: "ScoreHistoryId",
                principalTable: "ScoreHistories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Fines_Disputes_DisputeId",
                table: "Fines",
                column: "DisputeId",
                principalTable: "Disputes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemReviews_Items_ItemId",
                table: "ItemReviews",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreHistories_AspNetUsers_UserId",
                table: "ScoreHistories",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreHistories_Loans_LoanId",
                table: "ScoreHistories",
                column: "LoanId",
                principalTable: "Loans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_VerificationRequests_AspNetUsers_UserId",
                table: "VerificationRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
