using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SabEngine.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    WasApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedByUserId = table.Column<string>(type: "text", nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approvals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromState = table.Column<string>(type: "text", nullable: false),
                    ToState = table.Column<string>(type: "text", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PreviousEntryHash = table.Column<string>(type: "text", nullable: true),
                    Hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "execution_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<string>(type: "text", nullable: false),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    RollbackFired = table.Column<bool>(type: "boolean", nullable: false),
                    Output = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_execution_results", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Target = table.Column<string>(type: "text", nullable: false),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    Text = table.Column<string>(type: "text", nullable: false),
                    AuthorUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Steps = table.Column<string>(type: "jsonb", nullable: false),
                    Reasoning = table.Column<string>(type: "text", nullable: false),
                    IsFlaggedUnusual = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "target_state",
                columns: table => new
                {
                    Target = table.Column<string>(type: "text", nullable: false),
                    Facts = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_target_state", x => x.Target);
                });

            migrationBuilder.CreateTable(
                name: "workflow_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<string>(type: "text", nullable: false),
                    Target = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClaimedByWorkerId = table.Column<string>(type: "text", nullable: true),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_runs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_approvals_PlanId",
                table: "approvals",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_Timestamp",
                table: "audit_entries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_WorkflowRunId",
                table: "audit_entries",
                column: "WorkflowRunId");

            migrationBuilder.CreateIndex(
                name: "IX_execution_results_WorkflowRunId",
                table: "execution_results",
                column: "WorkflowRunId");

            migrationBuilder.CreateIndex(
                name: "IX_notes_Target",
                table: "notes",
                column: "Target");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_runs_State",
                table: "workflow_runs",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_runs_WorkflowId_Target",
                table: "workflow_runs",
                columns: new[] { "WorkflowId", "Target" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approvals");

            migrationBuilder.DropTable(
                name: "audit_entries");

            migrationBuilder.DropTable(
                name: "execution_results");

            migrationBuilder.DropTable(
                name: "notes");

            migrationBuilder.DropTable(
                name: "plans");

            migrationBuilder.DropTable(
                name: "target_state");

            migrationBuilder.DropTable(
                name: "workflow_runs");
        }
    }
}
