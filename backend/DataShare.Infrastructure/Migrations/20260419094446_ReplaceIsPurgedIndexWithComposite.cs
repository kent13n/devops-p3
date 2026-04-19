using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataShare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsPurgedIndexWithComposite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredFiles_IsPurged",
                table: "StoredFiles");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_IsPurged_ExpiresAt",
                table: "StoredFiles",
                columns: new[] { "IsPurged", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredFiles_IsPurged_ExpiresAt",
                table: "StoredFiles");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_IsPurged",
                table: "StoredFiles",
                column: "IsPurged");
        }
    }
}
