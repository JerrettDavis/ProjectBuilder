using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectBuilder.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialProjectCreation : Migration
{
    private static readonly string[] ProjectNameIndexColumns = ["WorkspaceId", "NormalizedName"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "projects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Purpose = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                IntendedOutcome = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                CurrentRevision = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_projects", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "project_change_sets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                RequestFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ResultRevision = table.Column<long>(type: "bigint", nullable: false),
                Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_project_change_sets", x => x.Id);
                table.ForeignKey(
                    name: "FK_project_change_sets_projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_project_change_sets_ProjectId",
            table: "project_change_sets",
            column: "ProjectId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_projects_workspace_normalized_name",
            table: "projects",
            columns: ProjectNameIndexColumns,
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "project_change_sets");

        migrationBuilder.DropTable(
            name: "projects");
    }
}
