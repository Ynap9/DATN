using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Identity.Client;
using sh.be.domain.Module.Ai.Models.Entities;
using sh.be.domain.Module.Ai.ProjectConfigs.Entities;
using sh.be.domain.Module.Ai.Prompts.Entities;
using sh.be.domain.Module.Ai.Providers.Entities;
using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Core.Entities;
using sh.be.domain.Module.Construction.DocumentFields.Entities;
using sh.be.domain.Module.Construction.DocumentTypes.Entities;
using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.Submissions.Entities;
using sh.be.domain.Module.DigitalSignature.Entities;
using sh.be.domain.Module.Operation.Assignments.Entities;
using sh.be.domain.Module.Operation.ProjectMembers.Entities;
using sh.be.domain.Module.Operation.StaffGroups.Entities;
using sh.be.domain.Module.Packaging.Entities;
using sh.be.shared.Constants.Ai;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.DigitalSignature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.infrastructure.data.Persistence.Db
{
    public class ShDbContext: IdentityUserContext<AppUser>
    {
        public ShDbContext(DbContextOptions<ShDbContext> options) : base(options)
        {
        }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<DocumentField> DocumentFields { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectConfig> ProjectConfigs { get; set; }
        public DbSet<ProjectDocumentType> ProjectDocumentTypes { get; set; }
        public DbSet<ProjectDocumentTypeDocumentField> ProjectDocumentTypeDocumentFields { get; set; }
        public DbSet<ProjectDeadlineNotification> ProjectDeadlineNotifications { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<SubmissionData> SubmissionDatas { get; set; }
        public DbSet<SubmissionLog> SubmissionLogs { get; set; }
        public DbSet<SubmissionQaLog> SubmissionQaLogs { get; set; }
        public DbSet<SubmissionQaLogField> SubmissionQaLogFields { get; set; }
        public DbSet<SubmissionUser> SubmissionUsers { get; set; }
        public DbSet<QaSamplingConfig> QaSamplingConfigs { get; set; }
        public DbSet<QaReviewerMode> QaReviewerModes { get; set; }
        public DbSet<QaTicket> QaTickets { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<TemplatePosition> TemplatePositions { get; set; }
        public DbSet<SigningBatch> SigningBatches { get; set; }
        public DbSet<SigningBatchFile> SigningBatchFiles { get; set; }
        public DbSet<PackageSchema> PackageSchemas { get; set; }
        public DbSet<PackageSchemaField> PackageSchemaFields { get; set; }
        public DbSet<PackageCodeValue> PackageCodeValues { get; set; }
        public DbSet<StaffGroup> StaffGroups { get; set; }
        public DbSet<StaffGroupMember> StaffGroupMembers { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<ProjectPriceDefault> ProjectPriceDefaults { get; set; }
        public DbSet<ProjectMemberPrice> ProjectMemberPrices { get; set; }
        public DbSet<AssignmentTicket> AssignmentTickets { get; set; }
        public DbSet<AssignmentTicketItem> AssignmentTicketItems { get; set; }
        public DbSet<AiProvider> AiProviders { get; set; }
        public DbSet<AiModel> AiModels { get; set; }
        public DbSet<PromptTemplate> PromptTemplates { get; set; }
        public DbSet<PromptFieldConfig> PromptFieldConfigs { get; set; }
        public DbSet<ProjectAiConfig> ProjectAiConfigs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseOpenIddict();
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                
            }
            );
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");

            });
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });
            modelBuilder.Entity<DocumentType>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.Property(e => e.DefaultView).HasDefaultValue(1).HasSentinel(-1);
            });
            modelBuilder.Entity<DocumentField>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.Property(e => e.FieldType).HasDefaultValue("string");
                entity.Property(e => e.IsShowTableView).HasDefaultValue(true).HasSentinel(true);
                entity.Property(e => e.IsShowListView).HasDefaultValue(true).HasSentinel(true);
            });
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });
            modelBuilder.Entity<ProjectConfig>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });
            modelBuilder.Entity<ProjectDocumentType>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });
            modelBuilder.Entity<ProjectDocumentTypeDocumentField>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });
            modelBuilder.Entity<ProjectDeadlineNotification>(entity =>
            {
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex([nameof(ProjectDeadlineNotification.ProjectId), nameof(ProjectDeadlineNotification.NotificationType),
                        nameof(ProjectDeadlineNotification.EndedAt), nameof(ProjectDeadlineNotification.ToAddress)], ProjectDeadlineNotification.UniqueSentIndexName)
                    .HasFilter("[IsSuccess] = 1");
            });
            modelBuilder.Entity<Submission>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.Property(e => e.StatusCheck).HasDefaultValue(SubmissionConst.NOT_CHECKED);
                entity.HasIndex([nameof(Submission.ProjectId), nameof(Submission.DocumentTypeId), nameof(Submission.FileName),
                        nameof(Submission.ProfileRange), nameof(Submission.Box)], Submission.UniqueKeyIndexName)
                    .HasFilter("[Deleted] = 0");
            });
            modelBuilder.Entity<SubmissionData>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex([nameof(SubmissionData.SubmissionId), nameof(SubmissionData.DocumentFieldId)], SubmissionData.UniqueFieldIndexName)
                    .HasFilter("[Deleted] = 0");
            });
            modelBuilder.Entity<SubmissionLog>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });
            modelBuilder.Entity<SubmissionQaLog>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });
            modelBuilder.Entity<SubmissionQaLogField>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });
            modelBuilder.Entity<SubmissionUser>(entity =>
            {
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasOne<Submission>()
                    .WithMany()
                    .HasForeignKey(e => e.SubmissionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<QaSamplingConfig>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex([nameof(QaSamplingConfig.ProjectId)], QaSamplingConfig.UniqueProjectIndexName)
                    .HasFilter("[Deleted] = 0");
                entity.HasOne<Project>()
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<QaReviewerMode>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex([nameof(QaReviewerMode.ProjectId), nameof(QaReviewerMode.UserId)], QaReviewerMode.UniqueReviewerIndexName)
                    .HasFilter("[Deleted] = 0");
                entity.HasOne<Project>()
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<QaTicket>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasOne<Project>()
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<DocumentType>()
                    .WithMany()
                    .HasForeignKey(e => e.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Template>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.Property(e => e.IsShowDigitalSignature).HasDefaultValue(true).HasSentinel(true);
                entity.Property(e => e.StampIntensity).HasDefaultValue(TemplateConst.DEFAULT_INTENSITY).HasSentinel(-1);
                entity.Property(e => e.WetSignatureIntensity).HasDefaultValue(TemplateConst.DEFAULT_INTENSITY).HasSentinel(-1);
                entity.Property(e => e.WetSignatureStrokeWidth).HasDefaultValue(TemplateConst.DEFAULT_STROKE_WIDTH).HasSentinel(-1);
                entity.Property(e => e.DigitalSignatureColor).HasDefaultValue(TemplateConst.DEFAULT_DIGITAL_SIGNATURE_COLOR);
                entity.Property(e => e.DigitalSignatureFont).HasDefaultValue(TemplateConst.DEFAULT_DIGITAL_SIGNATURE_FONT);
                entity.Property(e => e.DigitalSignatureFontSize).HasDefaultValue(TemplateConst.DEFAULT_DIGITAL_SIGNATURE_FONT_SIZE).HasSentinel(-1d);
            });
            modelBuilder.Entity<TemplatePosition>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });
            modelBuilder.Entity<SigningBatch>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });
            modelBuilder.Entity<SigningBatchFile>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });
            modelBuilder.Entity<PackageSchema>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex([nameof(PackageSchema.PackageType), nameof(PackageSchema.MetadataType)], PackageSchema.UniqueTypeIndexName)
                    .HasFilter("[Deleted] = 0");
            });
            modelBuilder.Entity<PackageSchemaField>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex([nameof(PackageSchemaField.PackageSchemaId), nameof(PackageSchemaField.ElementName)], PackageSchemaField.UniqueElementIndexName)
                    .HasFilter("[Deleted] = 0");
                entity.HasOne<PackageSchema>()
                    .WithMany()
                    .HasForeignKey(e => e.PackageSchemaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<PackageCodeValue>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex([nameof(PackageCodeValue.CodeGroup), nameof(PackageCodeValue.Code)], PackageCodeValue.UniqueCodeIndexName)
                    .HasFilter("[Deleted] = 0");
            });
            modelBuilder.Entity<StaffGroup>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex([nameof(StaffGroup.OwnerUserId), nameof(StaffGroup.Name)], StaffGroup.UniqueOwnerNameIndexName)
                    .HasFilter("[Deleted] = 0");
            });
            modelBuilder.Entity<StaffGroupMember>(entity =>
            {
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasOne<StaffGroup>()
                    .WithMany()
                    .HasForeignKey(e => e.StaffGroupId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ProjectMember>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex([nameof(ProjectMember.ProjectId), nameof(ProjectMember.DocumentTypeId), nameof(ProjectMember.UserId)], ProjectMember.UniqueMemberIndexName)
                    .HasFilter("[Deleted] = 0");
                entity.HasOne<Project>()
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<DocumentType>()
                    .WithMany()
                    .HasForeignKey(e => e.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ProjectPriceDefault>(entity =>
            {
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.Property(e => e.EffectiveFrom).HasColumnType("date");
                entity.HasOne<Project>()
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<DocumentType>()
                    .WithMany()
                    .HasForeignKey(e => e.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ProjectMemberPrice>(entity =>
            {
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.Property(e => e.EffectiveFrom).HasColumnType("date");
                entity.HasOne<ProjectMember>()
                    .WithMany()
                    .HasForeignKey(e => e.ProjectMemberId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<AssignmentTicket>(entity =>
            {
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasOne<Project>()
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<DocumentType>()
                    .WithMany()
                    .HasForeignKey(e => e.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<AssignmentTicketItem>(entity =>
            {
                entity.HasOne<AssignmentTicket>()
                    .WithMany()
                    .HasForeignKey(e => e.AssignmentTicketId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<AiProvider>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex([nameof(AiProvider.Code)], AiProvider.UniqueCodeIndexName)
                    .HasFilter("[Deleted] = 0");
            });
            modelBuilder.Entity<AiModel>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex([nameof(AiModel.Code)], AiModel.UniqueCodeIndexName)
                    .HasFilter("[Deleted] = 0");
                entity.HasOne<AiProvider>()
                    .WithMany()
                    .HasForeignKey(e => e.ProviderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<PromptTemplate>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex([nameof(PromptTemplate.DocumentTypeId)], PromptTemplate.UniquePublishedIndexName)
                    .HasFilter($"[Status] = '{PromptTemplateConst.STATUS_PUBLISHED}' AND [Deleted] = 0");
                entity.HasOne<DocumentType>()
                    .WithMany()
                    .HasForeignKey(e => e.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<PromptFieldConfig>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex([nameof(PromptFieldConfig.PromptTemplateId), nameof(PromptFieldConfig.DocumentFieldId)], PromptFieldConfig.UniqueFieldIndexName)
                    .HasFilter("[Deleted] = 0");
                entity.HasIndex([nameof(PromptFieldConfig.PromptTemplateId), nameof(PromptFieldConfig.OutputKey)], PromptFieldConfig.UniqueOutputKeyIndexName)
                    .HasFilter("[Deleted] = 0");
                entity.HasOne<PromptTemplate>()
                    .WithMany()
                    .HasForeignKey(e => e.PromptTemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<DocumentField>()
                    .WithMany()
                    .HasForeignKey(e => e.DocumentFieldId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ProjectAiConfig>(entity =>
            {
                entity.HasQueryFilter(e => !e.Deleted);
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex([nameof(ProjectAiConfig.ProjectId), nameof(ProjectAiConfig.DocumentTypeId)], ProjectAiConfig.UniqueProjectDocumentTypeIndexName)
                    .HasFilter("[Deleted] = 0");
                entity.HasOne<Project>()
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<DocumentType>()
                    .WithMany()
                    .HasForeignKey(e => e.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<AiModel>()
                    .WithMany()
                    .HasForeignKey(e => e.PrimaryModelId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<AiModel>()
                    .WithMany()
                    .HasForeignKey(e => e.FallbackModelId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<PromptTemplate>()
                    .WithMany()
                    .HasForeignKey(e => e.PromptTemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionKey });
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });
            modelBuilder.Entity<ProjectDocumentType>()
                .HasKey(pd => new { pd.ProjectId, pd.DocumentTypeId });
            modelBuilder.Entity<ProjectDocumentTypeDocumentField>()
                .HasKey(pf => new { pf.ProjectId, pf.DocumentTypeId, pf.DocumentFieldId });
            modelBuilder.Entity<SubmissionQaLogField>()
                .HasKey(qf => new { qf.SubmissionQaLogId, qf.DocumentFieldId });
            base.OnModelCreating(modelBuilder);
        }
    }
}
