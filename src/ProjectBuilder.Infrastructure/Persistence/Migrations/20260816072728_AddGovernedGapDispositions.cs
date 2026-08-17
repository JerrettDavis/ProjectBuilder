using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectBuilder.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddGovernedGapDispositions : Migration
{
    private static readonly string[] DispositionScopeIndexColumns = ["ProjectId", "ProfileId", "RuleCode", "ScopeId"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "gap_dispositions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                ProfileId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                RuleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ScopeId = table.Column<Guid>(type: "uuid", nullable: false),
                Disposition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Rationale = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                Consequence = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                AuthorityActorId = table.Column<Guid>(type: "uuid", nullable: false),
                ReviewOn = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                TargetMilestone = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gap_dispositions", x => x.Id);
                table.ForeignKey(
                    name: "FK_gap_dispositions_projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_gap_dispositions_ProjectId_ProfileId_RuleCode_ScopeId",
            table: "gap_dispositions",
            columns: DispositionScopeIndexColumns,
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "gap_dispositions");
    }
}
