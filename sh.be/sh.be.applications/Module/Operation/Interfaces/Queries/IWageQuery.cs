using sh.be.applications.Module.Operation.Dtos.Wage;

namespace sh.be.applications.Module.Operation.Interfaces.Queries
{
    public interface IWageQuery
    {
        Task<List<WageMemberDto>> FindMembers(int projectId);
        Task<Dictionary<(int DocumentTypeId, string UserId), WageQuantityDto>> FindQuantities(int projectId);
    }
}
