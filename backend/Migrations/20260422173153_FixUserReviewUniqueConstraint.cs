using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class FixUserReviewUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserReviews_LoanId",
                table: "UserReviews");

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_LoanId_ReviewerId",
                table: "UserReviews",
                columns: new[] { "LoanId", "ReviewerId" },
                unique: true,
                filter: "[LoanId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserReviews_LoanId_ReviewerId",
                table: "UserReviews");

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_LoanId",
                table: "UserReviews",
                column: "LoanId",
                unique: true,
                filter: "[LoanId] IS NOT NULL");
        }
    }
}
