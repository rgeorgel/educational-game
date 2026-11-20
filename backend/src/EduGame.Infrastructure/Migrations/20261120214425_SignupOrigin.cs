using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SignupOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "signup_campaign",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "signup_medium",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "signup_referrer",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "signup_source",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_signup_source",
                table: "users",
                column: "signup_source");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_signup_source",
                table: "users");

            migrationBuilder.DropColumn(
                name: "signup_campaign",
                table: "users");

            migrationBuilder.DropColumn(
                name: "signup_medium",
                table: "users");

            migrationBuilder.DropColumn(
                name: "signup_referrer",
                table: "users");

            migrationBuilder.DropColumn(
                name: "signup_source",
                table: "users");
        }
    }
}
