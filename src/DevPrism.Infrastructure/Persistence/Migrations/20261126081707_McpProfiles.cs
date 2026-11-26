using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevPrism.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class McpProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mcp_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mcp_profiles", x => x.id);
                    table.CheckConstraint("ck_mcp_profiles_provider", "provider IN ('AzureDevOpsBoards','Figma')");
                    table.ForeignKey(
                        name: "FK_mcp_profiles_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mcp_profiles_project_id_provider",
                table: "mcp_profiles",
                columns: new[] { "project_id", "provider" },
                unique: true,
                filter: "project_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_mcp_profiles_provider",
                table: "mcp_profiles",
                column: "provider",
                unique: true,
                filter: "project_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mcp_profiles");
        }
    }
}
