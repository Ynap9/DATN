using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Construction
{
    /// <inheritdoc />
    public partial class Add_ProjectDocumentTypeDocumentField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectDocumentTypeDocumentField",
                schema: "construction",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    DocumentFieldId = table.Column<int>(type: "int", nullable: false),
                    FieldImport = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderFieldImport = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ProjectDocumentTypeDocumentField", x => new { x.ProjectId, x.DocumentTypeId, x.DocumentFieldId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocumentTypeDocumentField_DocumentFieldId",
                schema: "construction",
                table: "ProjectDocumentTypeDocumentField",
                column: "DocumentFieldId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectDocumentTypeDocumentField",
                schema: "construction");
        }
    }
}
