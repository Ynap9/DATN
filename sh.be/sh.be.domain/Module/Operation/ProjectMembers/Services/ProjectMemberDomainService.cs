using sh.be.domain.Module.Operation.ProjectMembers.Entities;
using sh.be.domain.Module.Operation.ProjectMembers.Models;
using sh.be.domain.Module.Operation.ProjectMembers.Repositories;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Operation.ProjectMembers.Services
{
    public class ProjectMemberDomainService
    {
        private readonly IProjectMemberRepository _projectMemberRepository;

        public ProjectMemberDomainService(IProjectMemberRepository projectMemberRepository)
        {
            _projectMemberRepository = projectMemberRepository;
        }

        public async Task EnsureManagerAsync(int projectId, int documentTypeId, string? managedByUserId)
        {
            if (managedByUserId != null && await _projectMemberRepository.FindActiveLeaderAsync(projectId, documentTypeId, managedByUserId) == null)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorLeaderNotMember);
            }
        }

        public string? ResolveSkipReason(string userId, string? role, Dictionary<string, List<string>> roleNamesByUser, HashSet<string> existingMemberUserIds)
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
            return null;
        }

        public string? ResolveGroupMemberRole(IReadOnlyCollection<string> roleNames)
        {
            return RoleConstants.EXCLUSIVE_STAFF_ROLES.FirstOrDefault(roleNames.Contains);
        }

        public async Task<RevokeOnRemoveResult> RevokeHeldDocumentsAsync(ProjectMember member, string? modifiedBy, DateTime now)
        {
            var revokedCount = 0;
            List<int> examinedTicketIds = [];
            List<int> cancelledAssignmentTicketIds = [];
            List<int> cancelledQaTicketIds = [];

            if (member.Role == RoleConstants.ROLE_DATA)
            {
                foreach (var ticket in await _projectMemberRepository.GetActiveAssignmentTicketsAsync(member.ProjectId, member.DocumentTypeId, member.UserId))
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
                    ticket.Cancel(modifiedBy, now);
                    if (await _projectMemberRepository.TryCancelQaTicketAsync(ticket.Id) == 0)
                    {
                        throw new UserFriendlyException(ErrorCodes.BadRequest, "Phiếu QA vừa thay đổi, hãy tải lại");
                    }
                    revokedCount += await _projectMemberRepository.ReleaseQaNotPassedAsync(ticket.Id);
                    cancelledQaTicketIds.Add(ticket.Id);
                }
            }

            var keptCount = examinedTicketIds.Count == 0 ? 0 : await _projectMemberRepository.CountKeptDocumentsAsync(member.Role, examinedTicketIds);
            return new RevokeOnRemoveResult(revokedCount, keptCount, cancelledAssignmentTicketIds, cancelledQaTicketIds);
        }
    }
}
