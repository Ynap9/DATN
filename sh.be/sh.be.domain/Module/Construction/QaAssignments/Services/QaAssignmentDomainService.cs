using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.QaAssignments.Repositories;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Construction.QaAssignments.Services
{
    public class QaAssignmentDomainService
    {
        private readonly IQaSamplingConfigRepository _qaSamplingConfigRepository;
        private readonly IQaReviewerModeRepository _qaReviewerModeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public QaAssignmentDomainService(IQaSamplingConfigRepository qaSamplingConfigRepository, IQaReviewerModeRepository qaReviewerModeRepository,
            IUserRepository userRepository, IRoleRepository roleRepository)
        {
            _qaSamplingConfigRepository = qaSamplingConfigRepository;
            _qaReviewerModeRepository = qaReviewerModeRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public async Task<QaSamplingConfig> GetConfigOrDefaultAsync(int projectId)
        {
            return await _qaSamplingConfigRepository.FindByProjectAsync(projectId) ?? QaSamplingConfig.Default(projectId);
        }

        public async Task<string> GetModeAsync(int projectId, string userId)
        {
            var reviewerMode = await _qaReviewerModeRepository.FindAsync(projectId, userId);
            return reviewerMode?.Mode ?? QaAssignmentConst.MODE_FULL;
        }

        public async Task<AppUser> EnsureQaReviewerAsync(string userId)
        {
            var user = await _userRepository.FindByIdAsync(userId)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);
            var roleNames = await _roleRepository.GetRoleNamesByUserIdAsync(user.Id);
            if (!roleNames.Contains(RoleConstants.ROLE_QA))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Người dùng không có vai trò QA");
            }
            return user;
        }
    }
}
