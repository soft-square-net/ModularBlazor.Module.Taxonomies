using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace soft.square.net.WebApi.Modules.Taxonomies.Infrastructure.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class InitialSchemes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "taxonomies");

            migrationBuilder.CreateTable(
                name: "schemes",
                schema: "taxonomies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "terms",
                schema: "taxonomies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConceptId = table.Column<int>(type: "int", nullable: true),
                    ConceptName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SchemeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_terms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_terms_schemes_SchemeId",
                        column: x => x.SchemeId,
                        principalSchema: "taxonomies",
                        principalTable: "schemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_schemes_TenantId",
                schema: "taxonomies",
                table: "schemes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_schemes_TenantId_Key",
                schema: "taxonomies",
                table: "schemes",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_terms_ConceptId",
                schema: "taxonomies",
                table: "terms",
                column: "ConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_terms_ParentId",
                schema: "taxonomies",
                table: "terms",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_terms_SchemeId_Slug",
                schema: "taxonomies",
                table: "terms",
                columns: new[] { "SchemeId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "terms",
                schema: "taxonomies");

            migrationBuilder.DropTable(
                name: "schemes",
                schema: "taxonomies");
        }
    }
}
