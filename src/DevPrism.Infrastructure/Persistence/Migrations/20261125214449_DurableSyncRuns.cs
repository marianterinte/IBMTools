using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevPrism.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DurableSyncRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sync_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_version = table.Column<long>(type: "bigint", nullable: false),
                    operation = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    state = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reason_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    active_slot = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_runs", x => x.id);
                    table.CheckConstraint("ck_sync_runs_operation", "operation IN ('Probe','Import')");
                    table.CheckConstraint("ck_sync_runs_slot", "active_slot = 1");
                    table.CheckConstraint("ck_sync_runs_state", "state IN ('Queued','Running','Succeeded','Partial','Failed','Interrupted','Cancelled')");
                    table.CheckConstraint("ck_sync_runs_version", "connection_version > 0");
                    table.ForeignKey(
                        name: "FK_sync_runs_connections_project_id_connection_id",
                        columns: x => new { x.project_id, x.connection_id },
                        principalTable: "connections",
                        principalColumns: new[] { "project_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sync_runs_project_id_connection_id",
                table: "sync_runs",
                columns: new[] { "project_id", "connection_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sync_runs_project_id_requested_at_id",
                table: "sync_runs",
                columns: new[] { "project_id", "requested_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_sync_runs_single_active",
                table: "sync_runs",
                column: "active_slot",
                unique: true,
                filter: "state IN ('Queued','Running')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sync_runs");
        }
    }
}
