using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Packaging
{
    /// <inheritdoc />
    public partial class Add_PackageSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackageDocument",
                schema: "packaging",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    DossierId = table.Column<int>(type: "int", nullable: true),
                    FileRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MetadataType = table.Column<int>(type: "int", nullable: true),
                    RelativePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ObjectKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DocKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsSigned = table.Column<bool>(type: "bit", nullable: false),
                    PdfAPart = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PageCount = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_PackageDocument", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackageDossier",
                schema: "packaging",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    ExcelDocumentId = table.Column<int>(type: "int", nullable: false),
                    PackageCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowIndex = table.Column<int>(type: "int", nullable: false),
                    FolderName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DocumentCount = table.Column<int>(type: "int", nullable: false),
                    PackageObjid = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PackageObjectKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PackageFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PackageSize = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_PackageDossier", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackageSession",
                schema: "packaging",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageType = table.Column<int>(type: "int", nullable: false),
                    MetadataTypes = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RootFolderName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalDocument = table.Column<int>(type: "int", nullable: false),
                    DoneDocument = table.Column<int>(type: "int", nullable: false),
                    ErrorDocument = table.Column<int>(type: "int", nullable: false),
                    WarningDocument = table.Column<int>(type: "int", nullable: false),
                    StartedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinishedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MatchReport = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiredDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BuildStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BuildTotal = table.Column<int>(type: "int", nullable: false),
                    BuildDone = table.Column<int>(type: "int", nullable: false),
                    BuildFailReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FondsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BuiltBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_PackageSession", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageDocument_SessionId",
                schema: "packaging",
                table: "PackageDocument",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageDossier_SessionId",
                schema: "packaging",
                table: "PackageDossier",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageDocument",
                schema: "packaging");

            migrationBuilder.DropTable(
                name: "PackageDossier",
                schema: "packaging");

            migrationBuilder.DropTable(
                name: "PackageSession",
                schema: "packaging");
        }
    }
}
