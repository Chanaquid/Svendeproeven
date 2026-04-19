using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class FixWarningsAndAverageRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "AverageRating",
                table: "Items",
                type: "float(4)",
                precision: 4,
                scale: 2,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "AverageRating",
                table: "Items",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float(4)",
                oldPrecision: 4,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
