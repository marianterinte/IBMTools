using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevPrism.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialProjectScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    scope_key = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    secret_ref = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connections", x => x.id);
                    table.UniqueConstraint("AK_connections_project_id_id", x => new { x.project_id, x.id });
                    table.ForeignKey(
                        name: "FK_connections_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_connections_project_id_provider_scope_key",
                table: "connections",
                columns: new[] { "project_id", "provider", "scope_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_owner_id_slug",
                table: "projects",
                columns: new[] { "owner_id", "slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "connections");

            migrationBuilder.DropTable(
                name: "projects");
        }
    }
}
