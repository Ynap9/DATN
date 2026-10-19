using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Construction
{
    /// <inheritdoc />
    public partial class Add_SubmissionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubmissionLog",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OldStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChangedDataCount = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submission_ProjectId_DocumentTypeId_StatusCheck",
                schema: "construction",
                table: "Submission",
                columns: new[] { "ProjectId", "DocumentTypeId", "StatusCheck" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionLog_SubmissionId_CreatedDate",
                schema: "construction",
                table: "SubmissionLog",
                columns: new[] { "SubmissionId", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmissionLog",
                schema: "construction");

            migrationBuilder.DropIndex(
                name: "IX_Submission_ProjectId_DocumentTypeId_StatusCheck",
                schema: "construction",
                table: "Submission");
        }
    }
}
