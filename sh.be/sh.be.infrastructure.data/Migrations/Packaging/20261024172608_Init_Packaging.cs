using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Packaging
{
    /// <inheritdoc />
    public partial class Init_Packaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "packaging");

            migrationBuilder.CreateTable(
                name: "PackageCodeValue",
                schema: "packaging",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodeGroup = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PackageCodeValue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackageSchema",
                schema: "packaging",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageType = table.Column<int>(type: "int", nullable: false),
                    MetadataType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(31)", maxLength: 31, nullable: false),
                    XsdFileName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MetadataFilePrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_PackageSchema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackageSchemaField",
                schema: "packaging",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageSchemaId = table.Column<int>(type: "int", nullable: false),
                    ElementName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    Requirement = table.Column<int>(type: "int", nullable: false),
                    ConditionElement = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConditionValues = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CodeGroup = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AllowMultipleValues = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PackageSchemaField", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageSchemaField_PackageSchema_PackageSchemaId",
                        column: x => x.PackageSchemaId,
                        principalSchema: "packaging",
                        principalTable: "PackageSchema",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageCodeValue_CodeGroup_Code",
                schema: "packaging",
                table: "PackageCodeValue",
                columns: new[] { "CodeGroup", "Code" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PackageSchema_PackageType_MetadataType",
                schema: "packaging",
                table: "PackageSchema",
                columns: new[] { "PackageType", "MetadataType" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PackageSchemaField_PackageSchemaId_ElementName",
                schema: "packaging",
                table: "PackageSchemaField",
                columns: new[] { "PackageSchemaId", "ElementName" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PackageSchemaField_PackageSchemaId_SortOrder",
                schema: "packaging",
                table: "PackageSchemaField",
                columns: new[] { "PackageSchemaId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageCodeValue",
                schema: "packaging");

            migrationBuilder.DropTable(
                name: "PackageSchemaField",
                schema: "packaging");

            migrationBuilder.DropTable(
                name: "PackageSchema",
                schema: "packaging");
        }
    }
}
