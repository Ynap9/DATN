using sh.be.domain.Module.Operation.StaffGroups.Entities;

namespace sh.be.domain.Module.Operation.StaffGroups.Repositories
{
    public interface IStaffGroupRepository
    {
        Task<StaffGroup?> FindByIdAsync(int id);
        Task<bool> ExistsNameAsync(string ownerUserId, string name, int? excludedId);
        Task AddAsync(StaffGroup staffGroup);
        Task<List<string>> GetMemberUserIdsAsync(int staffGroupId);
        Task AddMembersAsync(IEnumerable<StaffGroupMember> staffGroupMembers);
        Task RemoveMembersAsync(int staffGroupId, IEnumerable<string> userIds);
        Task RemoveAllMembersAsync(int staffGroupId);
        Task<Dictionary<string, List<string>>> GetStaffRoleNamesAsync(IEnumerable<string> userIds);
    }
}
