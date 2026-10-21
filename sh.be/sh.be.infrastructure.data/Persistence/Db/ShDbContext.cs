using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Identity.Client;
using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Core.Entities;
using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.DigitalSignature.Entities;
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
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<SubmissionData> SubmissionDatas { get; set; }
        public DbSet<SubmissionLog> SubmissionLogs { get; set; }
        public DbSet<SubmissionQaLog> SubmissionQaLogs { get; set; }
        public DbSet<SubmissionQaLogField> SubmissionQaLogFields { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<TemplatePosition> TemplatePositions { get; set; }
        public DbSet<SigningBatch> SigningBatches { get; set; }
        public DbSet<SigningBatchFile> SigningBatchFiles { get; set; }
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
