using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectBuilder.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddClaimsAndEvidence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "claims",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Statement = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                Status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ElementIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                EvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                TagsJson = table.Column<string>(type: "jsonb", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_claims", x => x.Id);
                table.ForeignKey(
                    name: "FK_claims_projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "evidence",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                Producer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                ProducedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ModelRevision = table.Column<long>(type: "bigint", nullable: false),
                Environment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Summary = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                LimitationsJson = table.Column<string>(type: "jsonb", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_evidence", x => x.Id);
                table.ForeignKey(
                    name: "FK_evidence_claims_ClaimId",
                    column: x => x.ClaimId,
                    principalTable: "claims",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_evidence_projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_claims_ProjectId",
            table: "claims",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_evidence_ClaimId",
            table: "evidence",
            column: "ClaimId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_evidence_ProjectId",
            table: "evidence",
            column: "ProjectId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "evidence");

        migrationBuilder.DropTable(
            name: "claims");
    }
}
