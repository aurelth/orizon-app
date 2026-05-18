using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orizon.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAppUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_briefing_entries_AppUser_UserId",
                table: "briefing_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_trello_board_configs_AppUser_UserId",
                table: "trello_board_configs");

            migrationBuilder.DropTable(
                name: "AppUser");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUser",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    GoogleAccessToken = table.Column<string>(type: "text", nullable: true),
                    GoogleConnectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GoogleRefreshToken = table.Column<string>(type: "text", nullable: true),
                    GoogleTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HasCompletedOnboarding = table.Column<bool>(type: "boolean", nullable: false),
                    IsTraveling = table.Column<bool>(type: "boolean", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    LocationName = table.Column<string>(type: "text", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    ProfilePictureUrl = table.Column<string>(type: "text", nullable: true),
                    ThemePreference = table.Column<int>(type: "integer", nullable: false),
                    Timezone = table.Column<string>(type: "text", nullable: false),
                    TravelLatitude = table.Column<double>(type: "double precision", nullable: true),
                    TravelLocationName = table.Column<string>(type: "text", nullable: true),
                    TravelLongitude = table.Column<double>(type: "double precision", nullable: true),
                    TrelloApiKey = table.Column<string>(type: "text", nullable: true),
                    TrelloEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TrelloToken = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUser", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_briefing_entries_AppUser_UserId",
                table: "briefing_entries",
                column: "UserId",
                principalTable: "AppUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_trello_board_configs_AppUser_UserId",
                table: "trello_board_configs",
                column: "UserId",
                principalTable: "AppUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
