using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Db;
using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Operation.ProjectMembers.Entities
{
    [Table(nameof(ProjectMember), Schema = DbSchemas.Operation)]
    [Index(nameof(ProjectId), nameof(DocumentTypeId), nameof(UserId), IsUnique = true, Name = UniqueMemberIndexName)]
    [Index(nameof(ProjectId), nameof(DocumentTypeId), nameof(Role), Name = $"IX_{nameof(ProjectMember)}_{nameof(ProjectId)}_{nameof(DocumentTypeId)}_{nameof(Role)}")]
    [Index(nameof(ManagedByUserId), Name = $"IX_{nameof(ProjectMember)}_{nameof(ManagedByUserId)}")]
    public class ProjectMember : ISoftDeleted
    {
        public const string UniqueMemberIndexName = $"IX_{nameof(ProjectMember)}_{nameof(ProjectId)}_{nameof(DocumentTypeId)}_{nameof(UserId)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int ProjectId { get; private set; }
        public int DocumentTypeId { get; private set; }
        [MaxLength(450)]
        public string UserId { get; private set; } = String.Empty;
        [MaxLength(ProjectMemberConst.ROLE_MAX_LENGTH)]
        public string Role { get; private set; } = String.Empty;
        [MaxLength(450)]
        public string? ManagedByUserId { get; private set; }
        public int? SourceStaffGroupId { get; private set; }
        public bool IsActive { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private ProjectMember()
        {
        }

        public static ProjectMember Create(int projectId, int documentTypeId, string userId, string role, string? managedByUserId,
            int? sourceStaffGroupId, string? createdBy, DateTime createdDate)
        {
            EnsureValidRole(role);
            return new ProjectMember
            {
                ProjectId = projectId,
                DocumentTypeId = documentTypeId,
                UserId = userId,
                Role = role,
                ManagedByUserId = role == RoleConstants.ROLE_LEADER ? null : managedByUserId,
                SourceStaffGroupId = sourceStaffGroupId,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void ChangeRole(string role, string? modifiedBy, DateTime modifiedDate)
        {
            EnsureValidRole(role);
            Role = role;
            if (role == RoleConstants.ROLE_LEADER)
            {
                ManagedByUserId = null;
            }
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void ChangeManager(string? managedByUserId, string? modifiedBy, DateTime modifiedDate)
        {
            ManagedByUserId = Role == RoleConstants.ROLE_LEADER ? null : managedByUserId;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Activate(string? modifiedBy, DateTime modifiedDate)
        {
            IsActive = true;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Deactivate(string? modifiedBy, DateTime modifiedDate)
        {
            IsActive = false;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Remove(string? deletedBy, DateTime deletedDate)
        {
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }

        public bool IsManagedBy(string leaderUserId)
        {
            return ManagedByUserId == leaderUserId;
        }

        private static void EnsureValidRole(string role)
        {
            if (!ProjectMemberConst.ROLES.Contains(role))
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorUserInvalidRole);
            }
        }
    }
}
