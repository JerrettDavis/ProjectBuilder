using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable IDE0161 // EF-generated migration shape.

namespace ProjectBuilder.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNarrativeHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentElementId",
                table: "model_elements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "narrative_payloads",
                columns: table => new
                {
                    ElementId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_narrative_payloads", x => x.ElementId);
                    table.ForeignKey(
                        name: "FK_narrative_payloads_model_elements_ElementId",
                        column: x => x.ElementId,
                        principalTable: "model_elements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_model_elements_ParentElementId",
                table: "model_elements",
                column: "ParentElementId");

            migrationBuilder.AddForeignKey(
                name: "FK_model_elements_model_elements_ParentElementId",
                table: "model_elements",
                column: "ParentElementId",
                principalTable: "model_elements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_model_elements_model_elements_ParentElementId",
                table: "model_elements");

            migrationBuilder.DropTable(
                name: "narrative_payloads");

            migrationBuilder.DropIndex(
                name: "IX_model_elements_ParentElementId",
                table: "model_elements");

            migrationBuilder.DropColumn(
                name: "ParentElementId",
                table: "model_elements");
        }
    }
}
