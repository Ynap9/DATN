using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Identity.Client;
using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Core.Entities;
using sh.be.domain.Module.Construction.Entities;
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
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionKey });
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });
            base.OnModelCreating(modelBuilder);
        }
    }
}
