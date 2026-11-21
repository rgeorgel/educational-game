using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StripeBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_subscriptions_external_id",
                table: "subscriptions");

            migrationBuilder.AddColumn<string>(
                name: "stripe_customer_id",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_status",
                table: "subscriptions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "external_updated_at",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_stripe_customer_id",
                table: "users",
                column: "stripe_customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_external_id",
                table: "subscriptions",
                column: "external_id",
                unique: true,
                filter: "external_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_stripe_customer_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_subscriptions_external_id",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "stripe_customer_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "external_status",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "external_updated_at",
                table: "subscriptions");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_external_id",
                table: "subscriptions",
                column: "external_id");
        }
    }
}
