using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Operation.StaffGroups.Entities;
using sh.be.domain.Module.Operation.StaffGroups.Repositories;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Operation.StaffGroups.Services
{
    public class StaffGroupDomainService
    {
        private readonly IStaffGroupRepository _staffGroupRepository;
        private readonly IUserRepository _userRepository;

        public StaffGroupDomainService(IStaffGroupRepository staffGroupRepository, IUserRepository userRepository)
        {
            _staffGroupRepository = staffGroupRepository;
            _userRepository = userRepository;
        }

        public async Task<StaffGroup> EnsureAccessibleAsync(int id, string currentUserId, bool isAdmin)
        {
            var staffGroup = await _staffGroupRepository.FindByIdAsync(id);
            if (staffGroup == null || (!isAdmin && !staffGroup.IsOwnedBy(currentUserId)))
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorStaffGroupNotFound);
            }
            return staffGroup;
        }

        public async Task EnsureUniqueNameAsync(string ownerUserId, string name, int? excludedId)
        {
            if (await _staffGroupRepository.ExistsNameAsync(ownerUserId, name, excludedId))
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorStaffGroupNameExists);
            }
        }

        public async Task EnsureSameStaffRoleAsync(IReadOnlyCollection<string> newUserIds, IReadOnlyCollection<string> existingUserIds)
        {
            var memberUserIds = existingUserIds.Concat(newUserIds).ToList();
            var staffRoleNames = await _staffGroupRepository.GetStaffRoleNamesAsync(memberUserIds);

            var invalidUserId = newUserIds.FirstOrDefault(userId => staffRoleNames.GetValueOrDefault(userId, []).Count == 0);
            if (invalidUserId != null)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorUserInvalidRole, $"Người dùng {invalidUserId} không có vai trò Data hoặc QA");
            }

            var groupRole = memberUserIds.SelectMany(userId => staffRoleNames.GetValueOrDefault(userId, [])).First();
            var mismatchedUserId = memberUserIds.FirstOrDefault(userId => staffRoleNames.GetValueOrDefault(userId, []).Any(roleName => roleName != groupRole));
            if (mismatchedUserId != null)
            {
                var mismatchedUser = await _userRepository.FindByIdAsync(mismatchedUserId);
                throw new UserFriendlyException(ErrorCodes.OperationErrorUserInvalidRole,
                    $"Người dùng {mismatchedUser?.UserName ?? mismatchedUserId} không cùng vai trò {groupRole} với nhóm");
            }
        }
    }
}
