using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FamilyAndPlanChoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_id",
                table: "subscriptions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "subscriptions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.CreateTable(
                name: "practice_goals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_practice_goals", x => x.id);
                    table.ForeignKey(
                        name: "fk_practice_goals_player_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_practice_goals_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_external_id",
                table: "subscriptions",
                column: "external_id");

            migrationBuilder.CreateIndex(
                name: "ix_practice_goals_profile_id_completed_at",
                table: "practice_goals",
                columns: new[] { "profile_id", "completed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_practice_goals_skill_id",
                table: "practice_goals",
                column: "skill_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "practice_goals");

            migrationBuilder.DropIndex(
                name: "ix_subscriptions_external_id",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "external_id",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "source",
                table: "subscriptions");
        }
    }
}
