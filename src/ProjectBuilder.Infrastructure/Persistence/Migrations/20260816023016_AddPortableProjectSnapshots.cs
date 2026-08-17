using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable IDE0161

namespace ProjectBuilder.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPortableProjectSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "portable_project_snapshots",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelRevision = table.Column<long>(type: "bigint", nullable: false),
                    FormatVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(71)", maxLength: 71, nullable: false),
                    CanonicalJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portable_project_snapshots", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_portable_project_snapshots_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "portable_project_snapshots");
        }
    }
}
