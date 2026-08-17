using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable IDE0161, CA1861 // EF-generated migration shape.

namespace ProjectBuilder.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTypedChangeOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OperationCount",
                table: "project_change_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SemanticSummary",
                table: "project_change_sets",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "project_change_operations",
                columns: table => new
                {
                    ChangeSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubjectKind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ElementId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_change_operations", x => new { x.ChangeSetId, x.Sequence });
                    table.CheckConstraint("ck_project_change_operations_sequence_nonnegative", "\"Sequence\" >= 0");
                    table.ForeignKey(
                        name: "FK_project_change_operations_project_change_sets_ChangeSetId",
                        column: x => x.ChangeSetId,
                        principalTable: "project_change_sets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO project_change_operations
                    ("ChangeSetId", "Sequence", "ProjectId", "Kind", "SubjectKind", "ElementId", "RelationId", "Summary", "PayloadJson")
                SELECT
                    "Id",
                    0,
                    "ProjectId",
                    "ChangeKind",
                    CASE WHEN "ChangeKind" = 'project.created' THEN 'project' ELSE "ChangeKind" END,
                    "ElementId",
                    NULL,
                    'Historical change recorded before typed operation persistence.',
                    jsonb_build_object('changeKind', "ChangeKind", 'elementId', "ElementId")
                FROM project_change_sets;

                UPDATE project_change_sets
                SET "OperationCount" = 1,
                    "SemanticSummary" = "ChangeKind" || ': migrated historical operation.';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_project_change_sets_operation_count_positive",
                table: "project_change_sets",
                sql: "\"OperationCount\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_project_change_operations_ProjectId_ChangeSetId",
                table: "project_change_operations",
                columns: new[] { "ProjectId", "ChangeSetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_project_change_sets_operation_count_positive",
                table: "project_change_sets");

            migrationBuilder.DropTable(
                name: "project_change_operations");

            migrationBuilder.DropColumn(
                name: "OperationCount",
                table: "project_change_sets");

            migrationBuilder.DropColumn(
                name: "SemanticSummary",
                table: "project_change_sets");
        }
    }
}
