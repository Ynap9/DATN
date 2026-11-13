using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Auth.Services;
using sh.be.domain.Module.Construction.DocumentTypes.Entities;
using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Construction.QaAssignments.Services;
using sh.be.domain.Module.Operation.ProjectMembers.Entities;
using sh.be.domain.Module.Operation.ProjectMembers.Repositories;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Operation.ProjectMembers.Services
{
    public class ProjectMemberDomainService
    {
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly QaAssignmentDomainService _qaAssignmentDomainService;

        public ProjectMemberDomainService(IProjectMemberRepository projectMemberRepository, IRoleRepository roleRepository,
            QaAssignmentDomainService qaAssignmentDomainService)
        {
            _projectMemberRepository = projectMemberRepository;
            _roleRepository = roleRepository;
            _qaAssignmentDomainService = qaAssignmentDomainService;
        }

        public static void EnsureValidPrice(decimal companyPrice, decimal actualPrice)
        {
            if (companyPrice < 0 || companyPrice > PriceConst.PRICE_MAX || actualPrice < 0 || actualPrice > companyPrice)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorPriceInvalid);
            }
        }

        public async Task<OperationAccess> EnsureCanManageAsync(int projectId, int documentTypeId, string currentUserId, bool requireOpenProject)
        {
            var project = await _projectMemberRepository.FindProjectAsync(projectId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
            var documentType = await _projectMemberRepository.FindDocumentTypeAsync(documentTypeId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentTypeNotFound);
            if (!await _projectMemberRepository.IsDocumentTypeInProjectAsync(projectId, documentTypeId))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentTypeNotInProject);
            }
            if (requireOpenProject && project.Status is ProjectConst.DONE or ProjectConst.EXPIRED)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorProjectClosed);
            }

            if (PermissionDomainService.IsAdmin(await _roleRepository.GetRoleNamesByUserIdAsync(currentUserId)))
            {
                return new OperationAccess(project, documentType, true, null);
            }
            _ = await _projectMemberRepository.FindActiveLeaderAsync(projectId, documentTypeId, currentUserId)
                ?? throw new UserFriendlyException(ErrorCodes.OperationErrorNotProjectLeader);
            return new OperationAccess(project, documentType, false, currentUserId);
        }

        public void EnsureCanTouchMember(OperationAccess access, ProjectMember member)
        {
            if (!access.IsAdmin && (member.Role == RoleConstants.ROLE_LEADER || !member.IsManagedBy(access.LeaderUserId!)))
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorLeaderForbiddenTarget);
            }
        }

        public async Task EnsureManagerAsync(int projectId, int documentTypeId, string? managedByUserId)
        {
            if (managedByUserId != null && await _projectMemberRepository.FindActiveLeaderAsync(projectId, documentTypeId, managedByUserId) == null)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorLeaderNotMember);
            }
        }

        // Dự án không QA thì không nhận vai trò QA (345); vai trò khác không chặn
        public static void EnsureRoleEnabled(Project project, string role)
        {
            if (role == RoleConstants.ROLE_QA)
            {
                project.EnsureQaEnabled();
            }
        }

        public string? ResolveSkipReason(string userId, string? role, Dictionary<string, List<string>> roleNamesByUser, HashSet<string> existingMemberUserIds,
            bool isQaEnabled)
        {
            if (!roleNamesByUser.TryGetValue(userId, out var roleNames))
            {
                return ProjectMemberConst.SKIP_REASON_USER_NOT_FOUND;
            }
            if (existingMemberUserIds.Contains(userId))
            {
                return ProjectMemberConst.SKIP_REASON_ALREADY_MEMBER;
            }
            if (role == null || !roleNames.Contains(role))
            {
                return ProjectMemberConst.SKIP_REASON_INVALID_ROLE;
            }
            if (role == RoleConstants.ROLE_QA && !isQaEnabled)
            {
                return ProjectMemberConst.SKIP_REASON_QA_DISABLED;
            }
            return null;
        }

        public string? ResolveGroupMemberRole(List<string> roleNames)
        {
            return RoleConstants.EXCLUSIVE_STAFF_ROLES.FirstOrDefault(roleNames.Contains);
        }

        public async Task<RevokeOnRemoveResult> RevokeHeldDocumentsAsync(ProjectMember member, string? modifiedBy, DateTime now)
        {
            var revokedCount = 0;
            List<int> examinedTicketIds = [];
            List<int> cancelledAssignmentTicketIds = [];
            List<int> cancelledQaTicketIds = [];
            List<int> cancelledQaOwnershipTicketIds = [];

            if (member.Role == RoleConstants.ROLE_DATA)
            {
                foreach (var ticket in await _projectMemberRepository.GetActiveAssignmentTicketsAsync(member.ProjectId, member.DocumentTypeId, member.UserId, RoleConstants.ROLE_DATA))
                {
                    examinedTicketIds.Add(ticket.Id);
                    var releasedCount = await _projectMemberRepository.ReleaseEntryNotCheckedAsync(ticket.Id);
                    if (releasedCount == 0)
                    {
                        continue;
                    }
                    revokedCount += releasedCount;
                    ticket.DecreaseDocuments(releasedCount, modifiedBy, now);
                    if (ticket.Status == AssignmentConst.STATUS_CANCELLED)
                    {
                        cancelledAssignmentTicketIds.Add(ticket.Id);
                    }
                }
            }
            else if (member.Role == RoleConstants.ROLE_QA)
            {
                foreach (var ticket in await _projectMemberRepository.GetOpenQaTicketsAsync(member.ProjectId, member.DocumentTypeId, member.UserId))
                {
                    examinedTicketIds.Add(ticket.Id);
                    revokedCount += await _qaAssignmentDomainService.CancelTicketAsync(ticket, modifiedBy, now);
                    cancelledQaTicketIds.Add(ticket.Id);
                }
                foreach (var ownershipTicket in await _projectMemberRepository.GetActiveAssignmentTicketsAsync(member.ProjectId, member.DocumentTypeId, member.UserId, RoleConstants.ROLE_QA))
                {
                    ownershipTicket.Cancel(modifiedBy, now);
                    cancelledQaOwnershipTicketIds.Add(ownershipTicket.Id);
                }
            }

            var keptCount = examinedTicketIds.Count == 0 ? 0 : await _projectMemberRepository.CountKeptDocumentsAsync(member.Role, examinedTicketIds);
            return new RevokeOnRemoveResult(revokedCount, keptCount, cancelledAssignmentTicketIds, cancelledQaTicketIds, cancelledQaOwnershipTicketIds);
        }
    }

    public record OperationAccess(Project Project, DocumentType DocumentType, bool IsAdmin, string? LeaderUserId);

    public record RevokeOnRemoveResult(int RevokedDocumentCount, int KeptDocumentCount, List<int> CancelledAssignmentTicketIds, List<int> CancelledQaTicketIds,
        List<int> CancelledQaOwnershipTicketIds);
}
