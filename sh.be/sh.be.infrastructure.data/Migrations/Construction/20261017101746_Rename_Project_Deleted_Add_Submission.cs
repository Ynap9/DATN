using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Construction
{
    /// <inheritdoc />
    public partial class Rename_Project_Deleted_Add_Submission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                schema: "construction",
                table: "Project",
                newName: "Deleted");

            migrationBuilder.CreateTable(
                name: "Submission",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileUrlGoogleDrive = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileUrlGstore = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileUrlS3Object = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ProfileRange = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Box = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StatusCheck = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_Submission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionData",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    DocumentFieldId = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndexRow = table.Column<int>(type: "int", nullable: true),
                    IndexGoogleSheet = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_SubmissionData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submission_FileName",
                schema: "construction",
                table: "Submission",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_Submission_ProjectId_DocumentTypeId_FileName_ProfileRange_Box",
                schema: "construction",
                table: "Submission",
                columns: new[] { "ProjectId", "DocumentTypeId", "FileName", "ProfileRange", "Box" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionData_ProjectId_DocumentTypeId_DocumentFieldId",
                schema: "construction",
                table: "SubmissionData",
                columns: new[] { "ProjectId", "DocumentTypeId", "DocumentFieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionData_SubmissionId_DocumentFieldId",
                schema: "construction",
                table: "SubmissionData",
                columns: new[] { "SubmissionId", "DocumentFieldId" },
                unique: true,
                filter: "[Deleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Submission",
                schema: "construction");

            migrationBuilder.DropTable(
                name: "SubmissionData",
                schema: "construction");

            migrationBuilder.RenameColumn(
                name: "Deleted",
                schema: "construction",
                table: "Project",
                newName: "IsDeleted");
        }
    }
}
