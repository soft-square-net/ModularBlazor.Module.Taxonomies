using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace soft.square.net.WebApi.Modules.Taxonomies.Infrastructure.Data.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class InitialSchemes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "taxonomies_schemes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TenantId = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    Key = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    Language = table.Column<string>(type: "NVARCHAR2(16)", maxLength: 16, nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    Version = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_taxonomies_schemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "taxonomies_terms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ParentId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    Position = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    ConceptId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    ConceptName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    SchemeId = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_taxonomies_terms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_taxonomies_terms_taxonomies_schemes_SchemeId",
                        column: x => x.SchemeId,
                        principalTable: "taxonomies_schemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_taxonomies_schemes_TenantId",
                table: "taxonomies_schemes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_taxonomies_schemes_TenantId_Key",
                table: "taxonomies_schemes",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_taxonomies_terms_ConceptId",
                table: "taxonomies_terms",
                column: "ConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_taxonomies_terms_ParentId",
                table: "taxonomies_terms",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_taxonomies_terms_SchemeId_Slug",
                table: "taxonomies_terms",
                columns: new[] { "SchemeId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "taxonomies_terms");

            migrationBuilder.DropTable(
                name: "taxonomies_schemes");
        }
    }
}
