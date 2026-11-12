using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Construction.Submissions.Entities;
using sh.be.domain.Module.Core.Repositories;

namespace sh.be.applications.Module.Operation.Implements
{
    // Luật phân việc chen vào luồng xem / kiểm / QA hồ sơ. Thân rỗng tới Task 15 (không đổi hành vi)
    public class SubmissionAssignmentService : BaseService, ISubmissionAssignmentService
    {
        public SubmissionAssignmentService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<SubmissionAssignmentService> logger,
            IRequestContext requestContext, TimeProvider timeProvider
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
        }

        // Trả người giữ mà danh sách phải lọc theo; null = không giới hạn
        public Task<string?> ResolveRestrictedUserIdAsync(int projectId, int documentTypeId, string userId)
        {
            return Task.FromResult<string?>(null);
        }

        // Chặn xem chi tiết / lịch sử văn bản không thuộc phạm vi người gọi
        public Task EnsureCanViewAsync(Submission submission, string userId)
        {
            return Task.CompletedTask;
        }

        // Chặn kiểm tra văn bản người gọi không giữ
        public Task EnsureCanCheckAsync(Submission submission, string userId)
        {
            return Task.CompletedTask;
        }

        // Đưa văn bản vừa kiểm xong về QA phụ trách, chạy trong transaction của Check
        public Task AssignCheckedToQaAsync(Submission submission, string userId, DateTime now)
        {
            return Task.CompletedTask;
        }

        // Cập nhật người giữ / phiếu theo kết quả QA, chạy trong transaction của ReviewQa
        public Task ApplyQaResultToAssignmentAsync(Submission submission, int newStatusQa, string userId, DateTime now)
        {
            return Task.CompletedTask;
        }
    }
}
