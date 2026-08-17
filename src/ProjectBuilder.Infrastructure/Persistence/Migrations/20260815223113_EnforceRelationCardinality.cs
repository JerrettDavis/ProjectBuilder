using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable IDE0161, CA1861 // EF-generated migration shape.

namespace ProjectBuilder.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceRelationCardinality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_model_relations_benefits_from_target",
                table: "model_relations",
                columns: new[] { "Kind", "TargetElementId" },
                unique: true,
                filter: "\"Kind\" = 'benefitsFrom'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_model_relations_benefits_from_target",
                table: "model_relations");
        }
    }
}
