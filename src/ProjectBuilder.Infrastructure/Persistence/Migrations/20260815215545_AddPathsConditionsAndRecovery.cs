using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable IDE0161 // EF-generated migration shape.

namespace ProjectBuilder.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPathsConditionsAndRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "path_payloads",
                columns: table => new
                {
                    ElementId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_path_payloads", x => x.ElementId);
                    table.ForeignKey(
                        name: "FK_path_payloads_model_elements_ElementId",
                        column: x => x.ElementId,
                        principalTable: "model_elements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "path_payloads");
        }
    }
}
