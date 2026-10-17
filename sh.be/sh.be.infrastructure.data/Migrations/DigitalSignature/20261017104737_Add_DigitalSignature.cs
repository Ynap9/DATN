using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.DigitalSignature
{
    /// <inheritdoc />
    public partial class Add_DigitalSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "digitalsignature");

            migrationBuilder.CreateTable(
                name: "SigningBatch",
                schema: "digitalsignature",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CertificateThumbprint = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalFiles = table.Column<int>(type: "int", nullable: false),
                    SignedFiles = table.Column<int>(type: "int", nullable: false),
                    FailedFiles = table.Column<int>(type: "int", nullable: false),
                    StopReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedFolderName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsProjectNotified = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_SigningBatch", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SigningBatchFile",
                schema: "digitalsignature",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SigningBatchId = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SourceObjectKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SignedObjectKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimestampedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSignatureValid = table.Column<bool>(type: "bit", nullable: true),
                    SignatureValidationMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_SigningBatchFile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Template",
                schema: "digitalsignature",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SigningReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SigningLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StampImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StampImageObjectKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WetSignatureImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WetSignatureImageObjectKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsShowDigitalSignature = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDigitalSignatureInImage = table.Column<bool>(type: "bit", nullable: false),
                    IsOverlaySigning = table.Column<bool>(type: "bit", nullable: false),
                    StampIntensity = table.Column<int>(type: "int", nullable: false, defaultValue: 140),
                    WetSignatureIntensity = table.Column<int>(type: "int", nullable: false, defaultValue: 140),
                    WetSignatureStrokeWidth = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    DigitalSignatureColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#000000"),
                    DigitalSignatureFont = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Times New Roman"),
                    DigitalSignatureFontSize = table.Column<double>(type: "float", nullable: false, defaultValue: 10.0),
                    WetSignatureColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
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
                    table.PrimaryKey("PK_Template", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplatePosition",
                schema: "digitalsignature",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    XRatio = table.Column<double>(type: "float", nullable: false),
                    YRatio = table.Column<double>(type: "float", nullable: false),
                    WidthRatio = table.Column<double>(type: "float", nullable: false),
                    HeightRatio = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_TemplatePosition", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SigningBatchFile_SigningBatchId_Status_Order",
                schema: "digitalsignature",
                table: "SigningBatchFile",
                columns: new[] { "SigningBatchId", "Status", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplatePosition_TemplateId",
                schema: "digitalsignature",
                table: "TemplatePosition",
                column: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SigningBatch",
                schema: "digitalsignature");

            migrationBuilder.DropTable(
                name: "SigningBatchFile",
                schema: "digitalsignature");

            migrationBuilder.DropTable(
                name: "Template",
                schema: "digitalsignature");

            migrationBuilder.DropTable(
                name: "TemplatePosition",
                schema: "digitalsignature");
        }
    }
}
