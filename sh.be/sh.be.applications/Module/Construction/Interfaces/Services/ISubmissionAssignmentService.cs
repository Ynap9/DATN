using sh.be.domain.Module.Construction.Submissions.Entities;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    // Điểm Thi công gọi vào luật phân việc; Vận hành (Operation) implement. Implement được SaveChanges, không mở / commit transaction.
    public interface ISubmissionAssignmentService
    {
        // null = xem được mọi văn bản của cặp; khác null = chỉ văn bản có SubmissionUser.UserId = giá trị này
        Task<string?> ResolveRestrictedUserIdAsync(int projectId, int documentTypeId, string userId);
        Task EnsureCanViewAsync(Submission submission, string userId);
        Task EnsureCanCheckAsync(Submission submission, string userId);
        Task AssignCheckedToQaAsync(Submission submission, string userId, DateTime now);
        Task ApplyQaResultToAssignmentAsync(Submission submission, int newStatusQa, string userId, DateTime now);
    }
}
