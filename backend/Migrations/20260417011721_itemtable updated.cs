using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class itemtableupdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
               name: "AverageRating",
               table: "Items",
               type: "float",
               nullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_Items_AverageRating",
                table: "Items",
                column: "AverageRating");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_AverageRating",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Items");
        }
    }
}
