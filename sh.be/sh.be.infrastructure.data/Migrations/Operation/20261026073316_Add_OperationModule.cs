using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Operation
{
    /// <inheritdoc />
    public partial class Add_OperationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "operation");

            migrationBuilder.CreateTable(
                name: "AssignmentTicket",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    AssigneeUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DocumentCount = table.Column<int>(type: "int", nullable: false),
                    SourceTicketId = table.Column<int>(type: "int", nullable: true),
                    DoneAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentTicket", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentTicket_DocumentType_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "construction",
                        principalTable: "DocumentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssignmentTicket_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "construction",
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMember",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ManagedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SourceStaffGroupId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_ProjectMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMember_DocumentType_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "construction",
                        principalTable: "DocumentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMember_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "construction",
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPriceDefault",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompanyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPriceDefault", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPriceDefault_DocumentType_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "construction",
                        principalTable: "DocumentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectPriceDefault_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "construction",
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffGroup",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
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
                    table.PrimaryKey("PK_StaffGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentTicketItem",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentTicketId = table.Column<int>(type: "int", nullable: false),
                    Box = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProfileRange = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentTicketItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentTicketItem_AssignmentTicket_AssignmentTicketId",
                        column: x => x.AssignmentTicketId,
                        principalSchema: "operation",
                        principalTable: "AssignmentTicket",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMemberPrice",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectMemberId = table.Column<int>(type: "int", nullable: false),
                    CompanyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMemberPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMemberPrice_ProjectMember_ProjectMemberId",
                        column: x => x.ProjectMemberId,
                        principalSchema: "operation",
                        principalTable: "ProjectMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffGroupMember",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffGroupId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffGroupMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffGroupMember_StaffGroup_StaffGroupId",
                        column: x => x.StaffGroupId,
                        principalSchema: "operation",
                        principalTable: "StaffGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submission_ProjectId_DocumentTypeId_Box_ProfileRange",
                schema: "construction",
                table: "Submission",
                columns: new[] { "ProjectId", "DocumentTypeId", "Box", "ProfileRange" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentTicket_DocumentTypeId",
                schema: "operation",
                table: "AssignmentTicket",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentTicket_ProjectId_DocumentTypeId_AssigneeUserId_Status",
                schema: "operation",
                table: "AssignmentTicket",
                columns: new[] { "ProjectId", "DocumentTypeId", "AssigneeUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentTicket_Status_DoneAt",
                schema: "operation",
                table: "AssignmentTicket",
                columns: new[] { "Status", "DoneAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentTicketItem_AssignmentTicketId",
                schema: "operation",
                table: "AssignmentTicketItem",
                column: "AssignmentTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMember_DocumentTypeId",
                schema: "operation",
                table: "ProjectMember",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMember_ManagedByUserId",
                schema: "operation",
                table: "ProjectMember",
                column: "ManagedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMember_ProjectId_DocumentTypeId_Role",
                schema: "operation",
                table: "ProjectMember",
                columns: new[] { "ProjectId", "DocumentTypeId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMember_ProjectId_DocumentTypeId_UserId",
                schema: "operation",
                table: "ProjectMember",
                columns: new[] { "ProjectId", "DocumentTypeId", "UserId" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMemberPrice_ProjectMemberId_EffectiveFrom",
                schema: "operation",
                table: "ProjectMemberPrice",
                columns: new[] { "ProjectMemberId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPriceDefault_DocumentTypeId",
                schema: "operation",
                table: "ProjectPriceDefault",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPriceDefault_ProjectId_DocumentTypeId_Role_EffectiveFrom",
                schema: "operation",
                table: "ProjectPriceDefault",
                columns: new[] { "ProjectId", "DocumentTypeId", "Role", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffGroup_OwnerUserId_Name",
                schema: "operation",
                table: "StaffGroup",
                columns: new[] { "OwnerUserId", "Name" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StaffGroupMember_StaffGroupId_UserId",
                schema: "operation",
                table: "StaffGroupMember",
                columns: new[] { "StaffGroupId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentTicketItem",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "ProjectMemberPrice",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "ProjectPriceDefault",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "StaffGroupMember",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "AssignmentTicket",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "ProjectMember",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "StaffGroup",
                schema: "operation");

            migrationBuilder.DropIndex(
                name: "IX_Submission_ProjectId_DocumentTypeId_Box_ProfileRange",
                schema: "construction",
                table: "Submission");
        }
    }
}
