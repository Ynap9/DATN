using sh.be.applications.Module.Construction.Dtos.QaAssignment;

namespace sh.be.applications.Module.Construction.Interfaces.Queries
{
    public interface IQaAssignmentQuery
    {
        Task<List<GetListQaReviewerModeDto>> FindReviewerModes(int projectId);
    }
}
