using sh.be.applications.Module.Construction.Dtos.QaAssignment;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    public interface IQaAssignmentService
    {
        Task<ViewQaSamplingConfigDto> FindConfig(int projectId);
        Task UpdateConfig(UpdateQaSamplingConfigDto dto);
        Task<List<GetListQaReviewerModeDto>> FindReviewerModes(int projectId);
        Task UpdateReviewerMode(UpdateQaReviewerModeDto dto);
    }
}
