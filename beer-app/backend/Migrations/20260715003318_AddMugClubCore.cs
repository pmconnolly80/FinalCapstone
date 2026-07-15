using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BeerApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMugClubCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StaffPins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PinHash = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffPins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Taverns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Taverns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BeerConfirmations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<string>(type: "text", nullable: false),
                    BeerId = table.Column<int>(type: "integer", nullable: false),
                    TavernId = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedByUserId = table.Column<string>(type: "text", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeerConfirmations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeerConfirmations_Beers_BeerId",
                        column: x => x.BeerId,
                        principalTable: "Beers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BeerConfirmations_Taverns_TavernId",
                        column: x => x.TavernId,
                        principalTable: "Taverns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeerConfirmations_BeerId",
                table: "BeerConfirmations",
                column: "BeerId");

            migrationBuilder.CreateIndex(
                name: "IX_BeerConfirmations_CustomerId_BeerId",
                table: "BeerConfirmations",
                columns: new[] { "CustomerId", "BeerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BeerConfirmations_TavernId",
                table: "BeerConfirmations",
                column: "TavernId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPins_UserId",
                table: "StaffPins",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeerConfirmations");

            migrationBuilder.DropTable(
                name: "StaffPins");

            migrationBuilder.DropTable(
                name: "Taverns");
        }
    }
}
