using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.applications.Module.Operation.Interfaces.Queries;
using sh.be.domain.Base;
using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.Submissions.Entities;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.Operation.Assignments.Repositories;
using sh.be.domain.Module.Operation.Assignments.Services;
using sh.be.domain.Module.Operation.ProjectMembers.Repositories;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.applications.Module.Operation.Implements
{
    // Luật phân việc chen vào luồng xem / kiểm / QA hồ sơ
    public class SubmissionAssignmentService : BaseService, ISubmissionAssignmentService
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IAssignmentQuery _assignmentQuery;
        private readonly AssignmentDomainService _assignmentDomainService;

        public SubmissionAssignmentService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<SubmissionAssignmentService> logger,
            IRequestContext requestContext, TimeProvider timeProvider,
            IAssignmentRepository assignmentRepository, IProjectMemberRepository projectMemberRepository, IAssignmentQuery assignmentQuery,
            AssignmentDomainService assignmentDomainService
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _assignmentRepository = assignmentRepository;
            _projectMemberRepository = projectMemberRepository;
            _assignmentQuery = assignmentQuery;
            _assignmentDomainService = assignmentDomainService;
        }

        // Trả người giữ mà danh sách phải lọc theo; null = không giới hạn
        public Task<string?> ResolveRestrictedUserIdAsync(int projectId, int documentTypeId, string userId)
        {
            _logger.LogInformation($"{nameof(ResolveRestrictedUserIdAsync)}, projectId={projectId}, documentTypeId={documentTypeId}, userId={userId}");
            return _assignmentDomainService.ResolveRestrictedUserIdAsync(projectId, documentTypeId, userId);
        }

        // Chặn xem chi tiết / lịch sử văn bản không thuộc phạm vi người gọi
        public Task EnsureCanViewAsync(Submission submission, string userId)
        {
            _logger.LogInformation($"{nameof(EnsureCanViewAsync)}, submissionId={submission.Id}, userId={userId}");
            return _assignmentDomainService.EnsureCanViewAsync(submission, userId);
        }

        // Chặn kiểm tra văn bản người gọi không giữ hoặc đã QA đạt
        public Task EnsureCanCheckAsync(Submission submission, string userId)
        {
            _logger.LogInformation($"{nameof(EnsureCanCheckAsync)}, submissionId={submission.Id}, userId={userId}");
            return _assignmentDomainService.EnsureCanCheckAsync(submission, userId);
        }

        // Đưa văn bản vừa kiểm xong vào đợt QA của QA phụ trách nút, chưa có đợt mở thì tạo đợt mới; dự án không QA thì chỉ xét chốt phiếu nhập liệu
        public async Task AssignCheckedToQaAsync(Submission submission, string userId, DateTime now)
        {
            _logger.LogInformation($"{nameof(AssignCheckedToQaAsync)}, submissionId={submission.Id}, userId={userId}");
            var project = await _projectMemberRepository.FindProjectAsync(submission.ProjectId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
            // Dự án không QA: kiểm xong là xong — không giao QA, phiếu nhập liệu của người giữ DONE khi mọi văn bản của phiếu đã kiểm
            if (!project.IsQaEnabled)
            {
                var dataHolder = await _assignmentRepository.FindHolderAsync(submission.Id, RoleConstants.ROLE_DATA);
                if (dataHolder != null)
                {
                    var dataTicket = await _assignmentRepository.FindTicketByIdAsync(dataHolder.TicketId)
                        ?? throw new UserFriendlyException(ErrorCodes.OperationErrorTicketNotFound);
                    await _assignmentDomainService.TryMarkDoneAsync(dataTicket, project.IsQaEnabled, now);
                }
                return;
            }

            // Kiểm lại người giữ sau khi Check đã ghi hồ sơ trong transaction: văn bản vừa bị thu hồi / chuyển giữa chừng thì 820, Check rollback
            await _assignmentDomainService.EnsureCanCheckAsync(submission, userId);
            var owner = await _assignmentDomainService.ResolveQaOwnerForCheckedAsync(submission);
            if (owner == null)
            {
                return;
            }

            var qaBatch = await _assignmentRepository.FindOpenQaBatchAsync(submission.ProjectId, submission.DocumentTypeId, owner.QaUserId, 1);
            if (qaBatch == null)
            {
                qaBatch = QaTicket.CreateFull(submission.ProjectId, submission.DocumentTypeId, owner.QaUserId, 1, userId, now);
                _assignmentRepository.AddQaTicket(qaBatch);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                qaBatch.AddDocuments(1, userId, now);
            }
            await _assignmentRepository.AddHoldersAsync([SubmissionUser.Create(submission.Id, submission.ProjectId, submission.DocumentTypeId,
                RoleConstants.ROLE_QA, owner.QaUserId, qaBatch.Id, userId, now)]);
            var ownershipTicket = await _assignmentRepository.FindTicketByIdAsync(owner.TicketId)
                ?? throw new UserFriendlyException(ErrorCodes.OperationErrorTicketNotFound);
            ownershipTicket.AddDocuments(1, userId, now);
        }

        // Cập nhật người giữ / phiếu nhập liệu theo kết quả QA: về pool khi cần, chốt phiếu DONE khi mọi văn bản đã đạt
        public async Task ApplyQaResultToAssignmentAsync(Submission submission, int newStatusQa, string userId, DateTime now)
        {
            _logger.LogInformation($"{nameof(ApplyQaResultToAssignmentAsync)}, submissionId={submission.Id}, newStatusQa={newStatusQa}, userId={userId}");
            var result = await _assignmentDomainService.ApplyQaResultAsync(submission, newStatusQa, userId, now);
            if (result == null)
            {
                return;
            }

            if (result.ReleaseReason != null)
            {
                var holderFullName = (await _assignmentQuery.FindFullNames([result.HolderUserId])).GetValueOrDefault(result.HolderUserId) ?? result.HolderUserId;
                AddAuditLog(AuditLogConstants.ACTION_ASSIGNMENT_RELEASE_TO_POOL, AuditLogConstants.ACTION_TYPE_DELETE,
                    $"Văn bản {submission.FileName} {result.ReleaseReason}, về pool (người nhập {holderFullName}, phiếu #{result.Ticket.Id})",
                    new { SubmissionId = submission.Id, result.HolderUserId, TicketId = result.Ticket.Id }, null);
                await _unitOfWork.SaveChangesAsync();
            }
            await _assignmentDomainService.TryMarkDoneAsync(result.Ticket, true, now);
        }
    }
}
