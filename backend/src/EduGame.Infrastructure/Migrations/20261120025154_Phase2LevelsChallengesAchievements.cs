using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2LevelsChallengesAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_game_sessions_stages_stage_id",
                table: "game_sessions");

            migrationBuilder.AddColumn<bool>(
                name: "placement_done",
                table: "subject_progress",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "calibrated_at",
                table: "problem_templates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "difficulty_offset",
                table: "problem_templates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "stage_id",
                table: "game_sessions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "level_id",
                table: "game_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "placement_estimate",
                table: "game_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "result_level_id",
                table: "game_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<List<int>>(
                name: "skill_ids",
                table: "game_sessions",
                type: "integer[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<int>(
                name: "subject_id",
                table: "game_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "answer",
                table: "attempts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            // Sessões da Fase 1: preenche tópico e habilidades a partir da etapa.
            migrationBuilder.Sql("""
                UPDATE game_sessions gs
                SET subject_id = r.subject_id, skill_ids = st.skill_ids
                FROM stages st JOIN regions r ON r.id = st.region_id
                WHERE gs.stage_id = st.id;
                """);

            migrationBuilder.CreateTable(
                name: "daily_activity",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    problems_solved = table.Column<int>(type: "integer", nullable: false),
                    correct = table.Column<int>(type: "integer", nullable: false),
                    xp = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_activity", x => new { x.profile_id, x.date });
                    table.ForeignKey(
                        name: "fk_daily_activity_player_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_achievements",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    earned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profile_achievements", x => new { x.profile_id, x.slug });
                    table.ForeignKey(
                        name: "fk_profile_achievements_player_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Atividade diária a partir das respostas já dadas (fuso de Brasília).
            migrationBuilder.Sql("""
                INSERT INTO daily_activity (profile_id, date, problems_solved, correct, xp)
                SELECT gs.profile_id, (a.created_at AT TIME ZONE 'America/Sao_Paulo')::date,
                       count(*), count(*) FILTER (WHERE a.is_correct), sum(a.xp_earned)
                FROM attempts a
                JOIN session_problems sp ON sp.id = a.session_problem_id
                JOIN game_sessions gs ON gs.id = sp.session_id
                GROUP BY 1, 2;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_game_sessions_level_id",
                table: "game_sessions",
                column: "level_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_sessions_result_level_id",
                table: "game_sessions",
                column: "result_level_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_sessions_subject_id",
                table: "game_sessions",
                column: "subject_id");

            migrationBuilder.AddForeignKey(
                name: "fk_game_sessions_levels_level_id",
                table: "game_sessions",
                column: "level_id",
                principalTable: "levels",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_game_sessions_levels_result_level_id",
                table: "game_sessions",
                column: "result_level_id",
                principalTable: "levels",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_game_sessions_stages_stage_id",
                table: "game_sessions",
                column: "stage_id",
                principalTable: "stages",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_game_sessions_subjects_subject_id",
                table: "game_sessions",
                column: "subject_id",
                principalTable: "subjects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_game_sessions_levels_level_id",
                table: "game_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_game_sessions_levels_result_level_id",
                table: "game_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_game_sessions_stages_stage_id",
                table: "game_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_game_sessions_subjects_subject_id",
                table: "game_sessions");

            migrationBuilder.DropTable(
                name: "daily_activity");

            migrationBuilder.DropTable(
                name: "profile_achievements");

            migrationBuilder.DropIndex(
                name: "ix_game_sessions_level_id",
                table: "game_sessions");

            migrationBuilder.DropIndex(
                name: "ix_game_sessions_result_level_id",
                table: "game_sessions");

            migrationBuilder.DropIndex(
                name: "ix_game_sessions_subject_id",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "placement_done",
                table: "subject_progress");

            migrationBuilder.DropColumn(
                name: "calibrated_at",
                table: "problem_templates");

            migrationBuilder.DropColumn(
                name: "difficulty_offset",
                table: "problem_templates");

            migrationBuilder.DropColumn(
                name: "level_id",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "placement_estimate",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "result_level_id",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "skill_ids",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "subject_id",
                table: "game_sessions");

            migrationBuilder.AlterColumn<int>(
                name: "stage_id",
                table: "game_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "answer",
                table: "attempts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddForeignKey(
                name: "fk_game_sessions_stages_stage_id",
                table: "game_sessions",
                column: "stage_id",
                principalTable: "stages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
