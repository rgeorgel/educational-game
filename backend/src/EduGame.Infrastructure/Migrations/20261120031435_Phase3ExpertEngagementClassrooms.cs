using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EduGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3ExpertEngagementClassrooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "accessory",
                table: "player_profiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "nenhum");

            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "player_profiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "padrao");

            migrationBuilder.AddColumn<DateOnly>(
                name: "daily_date",
                table: "game_sessions",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "classrooms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_classrooms", x => x.id);
                    table.ForeignKey(
                        name: "fk_classrooms_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "curated_problems",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    skill_id = table.Column<int>(type: "integer", nullable: false),
                    slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_curated_problems", x => x.id);
                    table.ForeignKey(
                        name: "fk_curated_problems_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "classroom_members",
                columns: table => new
                {
                    classroom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_classroom_members", x => new { x.classroom_id, x.profile_id });
                    table.ForeignKey(
                        name: "fk_classroom_members_classrooms_classroom_id",
                        column: x => x.classroom_id,
                        principalTable: "classrooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_classroom_members_player_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_game_sessions_profile_id_subject_id_daily_date",
                table: "game_sessions",
                columns: new[] { "profile_id", "subject_id", "daily_date" },
                unique: true,
                filter: "daily_date IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_classroom_members_profile_id",
                table: "classroom_members",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_classrooms_code",
                table: "classrooms",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_classrooms_owner_user_id",
                table: "classrooms",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_curated_problems_skill_id",
                table: "curated_problems",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "ix_curated_problems_slug",
                table: "curated_problems",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "classroom_members");

            migrationBuilder.DropTable(
                name: "curated_problems");

            migrationBuilder.DropTable(
                name: "classrooms");

            migrationBuilder.DropIndex(
                name: "ix_game_sessions_profile_id_subject_id_daily_date",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "accessory",
                table: "player_profiles");

            migrationBuilder.DropColumn(
                name: "color",
                table: "player_profiles");

            migrationBuilder.DropColumn(
                name: "daily_date",
                table: "game_sessions");
        }
    }
}
