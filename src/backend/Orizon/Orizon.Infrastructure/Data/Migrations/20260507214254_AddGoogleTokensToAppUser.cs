using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orizon.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleTokensToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleAccessToken",
                table: "AppUser",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GoogleConnectedAt",
                table: "AppUser",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleRefreshToken",
                table: "AppUser",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GoogleTokenExpiresAt",
                table: "AppUser",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleAccessToken",
                table: "AppUser");

            migrationBuilder.DropColumn(
                name: "GoogleConnectedAt",
                table: "AppUser");

            migrationBuilder.DropColumn(
                name: "GoogleRefreshToken",
                table: "AppUser");

            migrationBuilder.DropColumn(
                name: "GoogleTokenExpiresAt",
                table: "AppUser");
        }
    }
}
