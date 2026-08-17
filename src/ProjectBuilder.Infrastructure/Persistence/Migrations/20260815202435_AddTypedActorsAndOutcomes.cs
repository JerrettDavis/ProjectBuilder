using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable IDE0161, CA1861 // EF-generated migration shape.

namespace ProjectBuilder.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTypedActorsAndOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_project_change_sets_ProjectId",
                table: "project_change_sets");

            migrationBuilder.AddColumn<long>(
                name: "BaseRevision",
                table: "project_change_sets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChangeKind",
                table: "project_change_sets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ElementId",
                table: "project_change_sets",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE project_change_sets SET \"ChangeKind\" = 'project.created' WHERE \"ChangeKind\" = '';");

            migrationBuilder.CreateTable(
                name: "model_elements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    DefinitionStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KnowledgeStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_model_elements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_model_elements_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "actor_payloads",
                columns: table => new
                {
                    ElementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorKind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContextualRole = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    GoalsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ResponsibilitiesJson = table.Column<string>(type: "jsonb", nullable: false),
                    AuthorityJson = table.Column<string>(type: "jsonb", nullable: false),
                    ConstraintsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_actor_payloads", x => x.ElementId);
                    table.ForeignKey(
                        name: "FK_actor_payloads_model_elements_ElementId",
                        column: x => x.ElementId,
                        principalTable: "model_elements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "model_relations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceElementId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetElementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_model_relations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_model_relations_model_elements_SourceElementId",
                        column: x => x.SourceElementId,
                        principalTable: "model_elements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_model_relations_model_elements_TargetElementId",
                        column: x => x.TargetElementId,
                        principalTable: "model_elements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_model_relations_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outcome_payloads",
                columns: table => new
                {
                    ElementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Statement = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    SuccessSignalsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outcome_payloads", x => x.ElementId);
                    table.ForeignKey(
                        name: "FK_outcome_payloads_model_elements_ElementId",
                        column: x => x.ElementId,
                        principalTable: "model_elements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project_change_sets_ProjectId",
                table: "project_change_sets",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_model_elements_ProjectId_Order",
                table: "model_elements",
                columns: new[] { "ProjectId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_model_relations_Kind_SourceElementId_TargetElementId",
                table: "model_relations",
                columns: new[] { "Kind", "SourceElementId", "TargetElementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_model_relations_ProjectId",
                table: "model_relations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_model_relations_SourceElementId",
                table: "model_relations",
                column: "SourceElementId");

            migrationBuilder.CreateIndex(
                name: "IX_model_relations_TargetElementId",
                table: "model_relations",
                column: "TargetElementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "actor_payloads");

            migrationBuilder.DropTable(
                name: "model_relations");

            migrationBuilder.DropTable(
                name: "outcome_payloads");

            migrationBuilder.DropTable(
                name: "model_elements");

            migrationBuilder.DropIndex(
                name: "IX_project_change_sets_ProjectId",
                table: "project_change_sets");

            migrationBuilder.DropColumn(
                name: "BaseRevision",
                table: "project_change_sets");

            migrationBuilder.DropColumn(
                name: "ChangeKind",
                table: "project_change_sets");

            migrationBuilder.DropColumn(
                name: "ElementId",
                table: "project_change_sets");

            migrationBuilder.CreateIndex(
                name: "IX_project_change_sets_ProjectId",
                table: "project_change_sets",
                column: "ProjectId",
                unique: true);
        }
    }
}
