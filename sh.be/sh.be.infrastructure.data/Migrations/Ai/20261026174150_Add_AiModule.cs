using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Ai
{
    /// <inheritdoc />
    public partial class Add_AiModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ai");

            migrationBuilder.CreateTable(
                name: "AiProvider",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ApiKeyEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiKeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    HasApiKey = table.Column<bool>(type: "bit", nullable: false),
                    ApiKeyHeaderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApiKeyHeaderPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    MaxConcurrency = table.Column<int>(type: "int", nullable: false),
                    RateLimitPerMinute = table.Column<int>(type: "int", nullable: true),
                    ExtraHeadersJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_AiProvider", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptTemplate",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstructionPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SourceTemplateId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PublishedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_PromptTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptTemplate_DocumentType_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "construction",
                        principalTable: "DocumentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiModel",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RemoteModelName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Capabilities = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaxOutputTokens = table.Column<int>(type: "int", nullable: false),
                    DefaultParamsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RequestTemplateJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseMappingJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_AiModel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiModel_AiProvider_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "ai",
                        principalTable: "AiProvider",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromptFieldConfig",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromptTemplateId = table.Column<int>(type: "int", nullable: false),
                    DocumentFieldId = table.Column<int>(type: "int", nullable: false),
                    OutputKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Example = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NormalizeRule = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PromptFieldConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptFieldConfig_DocumentField_DocumentFieldId",
                        column: x => x.DocumentFieldId,
                        principalSchema: "construction",
                        principalTable: "DocumentField",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromptFieldConfig_PromptTemplate_PromptTemplateId",
                        column: x => x.PromptTemplateId,
                        principalSchema: "ai",
                        principalTable: "PromptTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectAiConfig",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    AiMode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PrimaryModelId = table.Column<int>(type: "int", nullable: false),
                    FallbackModelId = table.Column<int>(type: "int", nullable: true),
                    PromptTemplateId = table.Column<int>(type: "int", nullable: true),
                    ParamsOverrideJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExtraInstruction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PageRuleJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParallelLimit = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ProjectAiConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectAiConfig_AiModel_FallbackModelId",
                        column: x => x.FallbackModelId,
                        principalSchema: "ai",
                        principalTable: "AiModel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectAiConfig_AiModel_PrimaryModelId",
                        column: x => x.PrimaryModelId,
                        principalSchema: "ai",
                        principalTable: "AiModel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectAiConfig_DocumentType_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "construction",
                        principalTable: "DocumentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectAiConfig_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "construction",
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectAiConfig_PromptTemplate_PromptTemplateId",
                        column: x => x.PromptTemplateId,
                        principalSchema: "ai",
                        principalTable: "PromptTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiModel_Code",
                schema: "ai",
                table: "AiModel",
                column: "Code",
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AiModel_ProviderId",
                schema: "ai",
                table: "AiModel",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_AiProvider_Code",
                schema: "ai",
                table: "AiProvider",
                column: "Code",
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAiConfig_DocumentTypeId",
                schema: "ai",
                table: "ProjectAiConfig",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAiConfig_FallbackModelId",
                schema: "ai",
                table: "ProjectAiConfig",
                column: "FallbackModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAiConfig_PrimaryModelId",
                schema: "ai",
                table: "ProjectAiConfig",
                column: "PrimaryModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAiConfig_ProjectId_DocumentTypeId",
                schema: "ai",
                table: "ProjectAiConfig",
                columns: new[] { "ProjectId", "DocumentTypeId" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAiConfig_PromptTemplateId",
                schema: "ai",
                table: "ProjectAiConfig",
                column: "PromptTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptFieldConfig_DocumentFieldId",
                schema: "ai",
                table: "PromptFieldConfig",
                column: "DocumentFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptFieldConfig_PromptTemplateId_DocumentFieldId",
                schema: "ai",
                table: "PromptFieldConfig",
                columns: new[] { "PromptTemplateId", "DocumentFieldId" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PromptFieldConfig_PromptTemplateId_OutputKey",
                schema: "ai",
                table: "PromptFieldConfig",
                columns: new[] { "PromptTemplateId", "OutputKey" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplate_DocumentTypeId_Published",
                schema: "ai",
                table: "PromptTemplate",
                column: "DocumentTypeId",
                unique: true,
                filter: "[Status] = 'PUBLISHED' AND [Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplate_DocumentTypeId_Version",
                schema: "ai",
                table: "PromptTemplate",
                columns: new[] { "DocumentTypeId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectAiConfig",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "PromptFieldConfig",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "AiModel",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "PromptTemplate",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "AiProvider",
                schema: "ai");
        }
    }
}
