using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class itemtableupdatedv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_AverageRating",
                table: "Items");

            migrationBuilder.AlterColumn<double>(
                name: "AverageRating",
                table: "Items",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true,
                oldComputedColumnSql: "(SELECT AVG(CAST(r.Rating AS float)) FROM ItemReviews r WHERE r.ItemId = Id AND r.IsDeleted = 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "AverageRating",
                table: "Items",
                type: "float",
                nullable: true,
                computedColumnSql: "(SELECT AVG(CAST(r.Rating AS float)) FROM ItemReviews r WHERE r.ItemId = Id AND r.IsDeleted = 0)",
                stored: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_AverageRating",
                table: "Items",
                column: "AverageRating");
        }
    }
}
