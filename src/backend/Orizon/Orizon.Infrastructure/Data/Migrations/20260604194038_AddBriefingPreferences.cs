using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orizon.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBriefingPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BriefingHour",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "CalendarSectionEnabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EmailSectionEnabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TasksSectionEnabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TrelloSectionEnabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WeatherSectionEnabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BriefingHour",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CalendarSectionEnabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailSectionEnabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TasksSectionEnabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TrelloSectionEnabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "WeatherSectionEnabled",
                table: "users");
        }
    }
}
