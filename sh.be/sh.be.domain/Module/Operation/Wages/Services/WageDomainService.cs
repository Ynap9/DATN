using sh.be.shared.Constants.Auth;

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
    }

    public record WageMember(int DocumentTypeId, string UserId, string Role, string? ManagedByUserId, bool IsActive, bool IsRemoved);
}
