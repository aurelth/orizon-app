using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orizon.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrelloCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrelloApiKey",
                table: "AppUser",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrelloToken",
                table: "AppUser",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrelloApiKey",
                table: "AppUser");

            migrationBuilder.DropColumn(
                name: "TrelloToken",
                table: "AppUser");
        }
    }
}
