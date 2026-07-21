using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeerApi.Migrations
{
    /// <inheritdoc />
    public partial class AddBeerNerdStatsAndObdbBreweryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Abv",
                table: "Beers",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Class",
                table: "Beers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Ibu",
                table: "Beers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObdbBreweryId",
                table: "Beers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StyleFamily",
                table: "Beers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Abv",
                table: "Beers");

            migrationBuilder.DropColumn(
                name: "Class",
                table: "Beers");

            migrationBuilder.DropColumn(
                name: "Ibu",
                table: "Beers");

            migrationBuilder.DropColumn(
                name: "ObdbBreweryId",
                table: "Beers");

            migrationBuilder.DropColumn(
                name: "StyleFamily",
                table: "Beers");
        }
    }
}
