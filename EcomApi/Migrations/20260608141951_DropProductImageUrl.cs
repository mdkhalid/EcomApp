using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcomApi.Migrations
{
    /// <inheritdoc />
    public partial class DropProductImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ProductImages (ProductId, ImageUrl, SortOrder, CreatedAt)
                SELECT p.Id, p.ImageUrl, 1, GETUTCDATE()
                FROM Products p
                WHERE p.ImageUrl IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM ProductImages pi
                      WHERE pi.ProductId = p.Id AND pi.ImageUrl = p.ImageUrl
                  )
            ");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
