using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectBuilder.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCapabilities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "capability_payloads",
            columns: table => new
            {
                ElementId = table.Column<Guid>(type: "uuid", nullable: false),
                OutcomeIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                Priority = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_capability_payloads", x => x.ElementId);
                table.ForeignKey(
                    name: "FK_capability_payloads_model_elements_ElementId",
                    column: x => x.ElementId,
                    principalTable: "model_elements",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "capability_payloads");
    }
}
