using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataShare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurgedFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPurged",
                table: "StoredFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_IsPurged",
                table: "StoredFiles",
                column: "IsPurged");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredFiles_IsPurged",
                table: "StoredFiles");

            migrationBuilder.DropColumn(
                name: "IsPurged",
                table: "StoredFiles");
        }
    }
}
