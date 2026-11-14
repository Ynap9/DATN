using sh.be.shared.Constants.Auth;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Operation.Wages.Services
{
    public static class WageDomainService
    {
        public static HashSet<int> FindLeadingDocumentTypeIds(IEnumerable<WageMember> members, string userId)
        {
            return members
                .Where(m => m.Role == RoleConstants.ROLE_LEADER && m.IsActive && !m.IsRemoved && m.UserId == userId)
                .Select(m => m.DocumentTypeId)
                .ToHashSet();
        }

        public static bool CanViewCompanyPrice(WageMember member, string currentUserId, bool isAdmin, HashSet<int> leadingDocumentTypeIds)
        {
            return isAdmin || (leadingDocumentTypeIds.Contains(member.DocumentTypeId) && member.ManagedByUserId == currentUserId);
        }

        public static bool CanViewMember(WageMember member, string currentUserId, bool isAdmin, HashSet<int> leadingDocumentTypeIds)
        {
            return member.UserId == currentUserId || CanViewCompanyPrice(member, currentUserId, isAdmin, leadingDocumentTypeIds);
        }

        public static decimal CalculateAmount(int quantity, decimal? price)
        {
            return quantity * (price ?? 0m);
        }

        public static void EnsureCanExport(bool isAdmin)
        {
            if (!isAdmin)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Chỉ Admin được xuất Excel tiền công");
            }
        }
    }

    public record WageMember(int DocumentTypeId, string UserId, string Role, string? ManagedByUserId, bool IsActive, bool IsRemoved);
}
