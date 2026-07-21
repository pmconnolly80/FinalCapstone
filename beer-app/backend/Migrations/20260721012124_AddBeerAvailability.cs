using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeerApi.Migrations
{
    /// <inheritdoc />
    public partial class AddBeerAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Availability",
                table: "Beers",
                type: "text",
                nullable: false,
                defaultValue: "Available");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Availability",
                table: "Beers");
        }
    }
}
