using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeerApi.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUserMarketingConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MarketingConsent",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarketingConsent",
                table: "AspNetUsers");
        }
    }
}
