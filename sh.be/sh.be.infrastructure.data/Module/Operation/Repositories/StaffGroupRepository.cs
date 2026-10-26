using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Operation.StaffGroups.Entities;
using sh.be.domain.Module.Operation.StaffGroups.Repositories;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;

namespace sh.be.infrastructure.data.Module.Operation.Repositories
{
    public class StaffGroupRepository : IStaffGroupRepository
    {
        private readonly ShDbContext _shDbContext;

        public StaffGroupRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<StaffGroup?> FindByIdAsync(int id)
        {
            return _shDbContext.StaffGroups.FirstOrDefaultAsync(g => g.Id == id);
        }

        public Task<bool> ExistsNameAsync(string ownerUserId, string name, int? excludedId)
        {
            return _shDbContext.StaffGroups.AnyAsync(g => g.OwnerUserId == ownerUserId && g.Name == name && g.Id != excludedId);
        }

        public async Task AddAsync(StaffGroup staffGroup)
        {
            await _shDbContext.StaffGroups.AddAsync(staffGroup);
        }

        public Task<List<string>> GetMemberUserIdsAsync(int staffGroupId)
        {
            return _shDbContext.StaffGroupMembers
                .Where(m => m.StaffGroupId == staffGroupId)
                .Select(m => m.UserId)
                .ToListAsync();
        }

        public Task AddMembersAsync(IEnumerable<StaffGroupMember> staffGroupMembers)
        {
            return _shDbContext.StaffGroupMembers.AddRangeAsync(staffGroupMembers);
        }

        public async Task RemoveMembersAsync(int staffGroupId, IEnumerable<string> userIds)
        {
            var staffGroupMembers = await _shDbContext.StaffGroupMembers
                .Where(m => m.StaffGroupId == staffGroupId && userIds.Contains(m.UserId))
                .ToListAsync();
            _shDbContext.StaffGroupMembers.RemoveRange(staffGroupMembers);
        }

        public async Task RemoveAllMembersAsync(int staffGroupId)
        {
            var staffGroupMembers = await _shDbContext.StaffGroupMembers
                .Where(m => m.StaffGroupId == staffGroupId)
                .ToListAsync();
            _shDbContext.StaffGroupMembers.RemoveRange(staffGroupMembers);
        }

        public async Task<Dictionary<string, List<string>>> GetStaffRoleNamesAsync(IEnumerable<string> userIds)
        {
            var distinctUserIds = userIds.Distinct().ToList();
            var existingUserIds = await _shDbContext.Users
                .Where(u => distinctUserIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();
            var staffRoles = await (from ur in _shDbContext.UserRoles
                                    join r in _shDbContext.Roles on ur.RoleId equals r.Id
                                    where distinctUserIds.Contains(ur.UserId) && RoleConstants.EXCLUSIVE_STAFF_ROLES.Contains(r.Name)
                                    select new { ur.UserId, r.Name }).Distinct().ToListAsync();
            var roleNamesByUser = staffRoles.ToLookup(x => x.UserId, x => x.Name);
            return existingUserIds.ToDictionary(userId => userId, userId => roleNamesByUser[userId].OrderBy(name => name).ToList());
        }
    }
}
