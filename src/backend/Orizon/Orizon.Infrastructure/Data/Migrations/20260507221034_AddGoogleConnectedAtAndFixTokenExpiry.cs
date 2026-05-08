using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orizon.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleConnectedAtAndFixTokenExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GoogleTokenExpiry",
                table: "users",
                newName: "GoogleTokenExpiresAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "GoogleConnectedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleConnectedAt",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "GoogleTokenExpiresAt",
                table: "users",
                newName: "GoogleTokenExpiry");
        }
    }
}
