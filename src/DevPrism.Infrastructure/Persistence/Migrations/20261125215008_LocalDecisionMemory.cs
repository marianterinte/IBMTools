using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevPrism.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LocalDecisionMemory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "decision_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    statement = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    rationale = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    authored_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    state = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decision_versions", x => x.id);
                    table.CheckConstraint("ck_decision_approval", "(state = 'Draft' AND approved_by IS NULL AND approved_at IS NULL) OR (state IN ('Approved','Superseded') AND approved_by IS NOT NULL AND approved_at IS NOT NULL)");
                    table.CheckConstraint("ck_decision_revision", "revision > 0");
                    table.ForeignKey(
                        name: "FK_decision_versions_features_project_id_feature_id",
                        columns: x => new { x.project_id, x.feature_id },
                        principalTable: "features",
                        principalColumns: new[] { "project_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_decision_versions_project_id_feature_id",
                table: "decision_versions",
                columns: new[] { "project_id", "feature_id" });

            migrationBuilder.CreateIndex(
                name: "IX_decision_versions_project_id_key_revision",
                table: "decision_versions",
                columns: new[] { "project_id", "key", "revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_decision_versions_project_id_key_state",
                table: "decision_versions",
                columns: new[] { "project_id", "key", "state" },
                unique: true,
                filter: "state IN ('Draft','Approved')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "decision_versions");
        }
    }
}
