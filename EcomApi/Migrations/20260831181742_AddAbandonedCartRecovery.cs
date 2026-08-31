using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcomApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAbandonedCartRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AbandonedCartOptOut",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAbandonedCartNotifiedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastAbandonedCartNotifiedAt",
                table: "Users",
                column: "LastAbandonedCartNotifiedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_LastAbandonedCartNotifiedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AbandonedCartOptOut",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastAbandonedCartNotifiedAt",
                table: "Users");
        }
    }
}
