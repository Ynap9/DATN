using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Construction
{
    /// <inheritdoc />
    public partial class Add_SubmissionLog_ProjectIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SubmissionLog_ProjectId_DocumentTypeId_CreatedDate",
                schema: "construction",
                table: "SubmissionLog",
                columns: new[] { "ProjectId", "DocumentTypeId", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmissionLog_ProjectId_DocumentTypeId_CreatedDate",
                schema: "construction",
                table: "SubmissionLog");
        }
    }
}
