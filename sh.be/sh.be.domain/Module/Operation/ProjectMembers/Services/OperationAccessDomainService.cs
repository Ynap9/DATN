using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Operation.ProjectMembers.Entities;
using sh.be.domain.Module.Operation.ProjectMembers.Models;
using sh.be.domain.Module.Operation.ProjectMembers.Repositories;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Operation.ProjectMembers.Services
{
    public class OperationAccessDomainService
    {
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IRoleRepository _roleRepository;

        public OperationAccessDomainService(IProjectMemberRepository projectMemberRepository, IRoleRepository roleRepository)
        {
            _projectMemberRepository = projectMemberRepository;
            _roleRepository = roleRepository;
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

            if (OperationAccessPolicy.IsAdmin(await _roleRepository.GetRoleNamesByUserIdAsync(currentUserId)))
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
    }
}
