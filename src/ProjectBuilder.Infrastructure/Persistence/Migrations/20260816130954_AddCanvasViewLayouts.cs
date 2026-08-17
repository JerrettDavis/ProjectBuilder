using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectBuilder.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCanvasViewLayouts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "canvas_views",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Lens = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ScopeKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                OwnerKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ModelRevision = table.Column<long>(type: "bigint", nullable: false),
                LayoutVersion = table.Column<long>(type: "bigint", nullable: false),
                LayoutJson = table.Column<string>(type: "jsonb", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_canvas_views", x => x.Id);
                table.ForeignKey(
                    name: "FK_canvas_views_projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_canvas_views_ProjectId_Lens_ScopeKey_Visibility_OwnerKey",
            table: "canvas_views",
            columns: ["ProjectId", "Lens", "ScopeKey", "Visibility", "OwnerKey"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "canvas_views");
    }
}
