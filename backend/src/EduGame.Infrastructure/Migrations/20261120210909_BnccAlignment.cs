using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BnccAlignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "bncc_codes",
                table: "skills",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.CreateTable(
                name: "bncc_skills",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    component = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    grade = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    grade_order = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bncc_skills", x => x.code);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bncc_skills");

            migrationBuilder.DropColumn(
                name: "bncc_codes",
                table: "skills");
        }
    }
}
