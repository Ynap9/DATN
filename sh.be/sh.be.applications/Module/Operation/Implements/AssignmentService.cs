using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Module.Operation.Dtos.Assignment;
using sh.be.applications.Module.Operation.Interfaces.Queries;
using sh.be.applications.Module.Operation.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.Submissions.Entities;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.Operation.Assignments.Entities;
using sh.be.domain.Module.Operation.Assignments.Repositories;
using sh.be.domain.Module.Operation.Assignments.Services;
using sh.be.domain.Module.Operation.ProjectMembers.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Core;
using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Operation.Implements
{
    public class AssignmentService : BaseService, IAssignmentService
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IAssignmentQuery _assignmentQuery;
        private readonly AssignmentDomainService _assignmentDomainService;
        private readonly ProjectMemberDomainService _projectMemberDomainService;

        public AssignmentService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<AssignmentService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IAssignmentRepository assignmentRepository, IAssignmentQuery assignmentQuery, AssignmentDomainService assignmentDomainService,
            ProjectMemberDomainService projectMemberDomainService
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _assignmentRepository = assignmentRepository;
            _assignmentQuery = assignmentQuery;
            _assignmentDomainService = assignmentDomainService;
            _projectMemberDomainService = projectMemberDomainService;
        }

        public async Task<BaseResponsePagingDto<AssignmentNodeDto>> FindTree(int projectId, int documentTypeId, FindAssignmentTreeDto dto)
        {
            _logger.LogInformation($"{nameof(FindTree)}, projectId={projectId}, documentTypeId={documentTypeId}, dto={JsonSerializer.Serialize(dto)}");
            AssignmentDomainService.EnsureAssignableRole(dto.Role);
            if (dto.PageSize <= 0 || dto.PageSize > AssignmentConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {AssignmentConst.MAX_PAGE_SIZE}");
            }
            await _projectMemberDomainService.EnsureCanManageAsync(projectId, documentTypeId, getCurrentUserId(), false);
            return await _assignmentQuery.FindBoxNodes(projectId, documentTypeId, dto);
        }

        public async Task<BaseResponsePagingDto<AssignmentNodeDto>> FindProfiles(int projectId, int documentTypeId, FindAssignmentProfilesDto dto)
        {
            _logger.LogInformation($"{nameof(FindProfiles)}, projectId={projectId}, documentTypeId={documentTypeId}, dto={JsonSerializer.Serialize(dto)}");
            AssignmentDomainService.EnsureAssignableRole(dto.Role);
            if (dto.PageSize == 0)
            {
                dto.PageSize = AssignmentConst.PROFILE_PAGE_SIZE_DEFAULT;
            }
            if (dto.PageSize < 0 || dto.PageSize > AssignmentConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {AssignmentConst.MAX_PAGE_SIZE}");
            }
            await _projectMemberDomainService.EnsureCanManageAsync(projectId, documentTypeId, getCurrentUserId(), false);
            return await _assignmentQuery.FindProfileNodes(projectId, documentTypeId, dto);
        }

        public async Task<AssignmentPreviewDto> Preview(int projectId, int documentTypeId, AssignmentRequestDto dto)
        {
            _logger.LogInformation($"{nameof(Preview)}, projectId={projectId}, documentTypeId={documentTypeId}, dto={JsonSerializer.Serialize(dto)}");
            var access = await _projectMemberDomainService.EnsureCanManageAsync(projectId, documentTypeId, getCurrentUserId(), true);
            var plan = await _assignmentDomainService.PlanAsync(access, dto.Role, dto.AssigneeUserId,
                dto.Items.Select(item => new AssignmentItem(item.Box, item.ProfileRange)).ToList());

            var blockErrors = AssignmentDomainService.ResolveBlockErrors(plan);
            var holderNames = await _assignmentQuery.FindFullNames(plan.Conflicts.Select(conflict => conflict.HolderUserId).Distinct().ToList());
            return new AssignmentPreviewDto
            {
                Role = plan.Role,
                TotalDocuments = plan.TotalDocuments,
                NewDocuments = plan.NewSubmissionIds.Count,
                AlreadyHeldDocuments = plan.AlreadyHeldDocuments,
                ConflictDocuments = plan.ConflictDocuments,
                LaterDocuments = plan.LaterDocuments,
                SameAsEntryDocuments = plan.SameAsEntryDocuments,
                ReturnedToSameDocuments = plan.ReturnedToSameDocuments,
                Conflicts = plan.Conflicts
                    .Select(conflict => new AssignmentConflictDto
                    {
                        Box = conflict.Box,
                        ProfileRange = conflict.ProfileRange,
                        HolderUserId = conflict.HolderUserId,
                        HolderFullName = holderNames.GetValueOrDefault(conflict.HolderUserId),
                        DocumentCount = conflict.DocumentCount,
                        IsOwner = conflict.IsOwner,
                    })
                    .ToList(),
                NormalizedItems = plan.Nodes.Select(node => new AssignmentItemDto { Box = node.Box, ProfileRange = node.ProfileRange }).ToList(),
                EmptyItems = plan.EmptyItems.Select(item => new AssignmentItemDto { Box = item.Box, ProfileRange = item.ProfileRange }).ToList(),
                CanSave = blockErrors.Count == 0,
                BlockReasons = blockErrors.Select(error => error.Message).ToList(),
                Warnings = plan.ReturnedToSameDocuments > 0
                    ? [$"{plan.ReturnedToSameDocuments} văn bản từng về pool từ chính người nhận"]
                    : [],
            };
        }

        public async Task<ViewAssignmentTicketDto> Assign(int projectId, int documentTypeId, AssignmentRequestDto dto)
        {
            _logger.LogInformation($"{nameof(Assign)}, projectId={projectId}, documentTypeId={documentTypeId}, dto={JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var access = await _projectMemberDomainService.EnsureCanManageAsync(projectId, documentTypeId, currentUserId, true);
            var items = dto.Items.Select(item => new AssignmentItem(item.Box, item.ProfileRange)).ToList();
            var assigneeFullName = (await _assignmentQuery.FindFullNames([dto.AssigneeUserId])).GetValueOrDefault(dto.AssigneeUserId) ?? dto.AssigneeUserId;
            var now = GetVietnamTime();

            AssignmentTicket ticket;
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _assignmentRepository.LockPairAsync(projectId, documentTypeId);
                var plan = await _assignmentDomainService.PlanAsync(access, dto.Role, dto.AssigneeUserId, items);
                AssignmentDomainService.EnsurePlanSavable(plan);

                if (plan.Role == RoleConstants.ROLE_DATA)
                {
                    ticket = AssignmentTicket.CreateManual(projectId, documentTypeId, plan.AssigneeUserId, RoleConstants.ROLE_DATA,
                        plan.NewSubmissionIds.Count, null, currentUserId, now);
                    _assignmentRepository.AddTicket(ticket);
                    await _unitOfWork.SaveChangesAsync();

                    _assignmentRepository.AddTicketItems(plan.Nodes.Select(node => AssignmentTicketItem.Create(ticket.Id, node.Box, node.ProfileRange)).ToList());
                    await _assignmentRepository.AddHoldersAsync(plan.NewSubmissionIds
                        .Select(submissionId => SubmissionUser.CreateEntry(submissionId, projectId, documentTypeId, plan.AssigneeUserId, ticket.Id, currentUserId, now))
                        .ToList());
                    AddAuditLog(AuditLogConstants.ACTION_ASSIGNMENT_CREATE, AuditLogConstants.ACTION_TYPE_CREATE,
                        $"Giao {plan.NewSubmissionIds.Count} văn bản nhập liệu cho {assigneeFullName}, phiếu #{ticket.Id}, dự án {access.Project.Name}",
                        null, new { ticket.Id, ticket.Role, ticket.AssigneeUserId, plan.Nodes, SubmissionIds = plan.NewSubmissionIds });
                }
                else
                {
                    var ownershipTicket = plan.OwnershipTicketId == null ? null : await _assignmentRepository.FindTicketByIdAsync(plan.OwnershipTicketId.Value);
                    if (ownershipTicket == null)
                    {
                        ownershipTicket = AssignmentTicket.CreateManual(projectId, documentTypeId, plan.AssigneeUserId, RoleConstants.ROLE_QA,
                            plan.NewSubmissionIds.Count, null, currentUserId, now);
                        _assignmentRepository.AddTicket(ownershipTicket);
                        await _unitOfWork.SaveChangesAsync();
                    }
                    else
                    {
                        ownershipTicket.AddDocuments(plan.NewSubmissionIds.Count, currentUserId, now);
                    }
                    ticket = ownershipTicket;
                    _assignmentRepository.AddTicketItems(plan.NewOwnedNodes.Select(node => AssignmentTicketItem.Create(ticket.Id, node.Box, node.ProfileRange)).ToList());

                    int? qaBatchId = null;
                    if (plan.NewSubmissionIds.Count > 0)
                    {
                        var qaBatch = await _assignmentRepository.FindOpenQaBatchAsync(projectId, documentTypeId, plan.AssigneeUserId, plan.NewSubmissionIds.Count);
                        if (qaBatch == null)
                        {
                            qaBatch = QaTicket.CreateFull(projectId, documentTypeId, plan.AssigneeUserId, plan.NewSubmissionIds.Count, currentUserId, now);
                            _assignmentRepository.AddQaTicket(qaBatch);
                            await _unitOfWork.SaveChangesAsync();
                        }
                        else
                        {
                            qaBatch.AddDocuments(plan.NewSubmissionIds.Count, currentUserId, now);
                        }
                        qaBatchId = qaBatch.Id;
                        await _assignmentRepository.AddHoldersAsync(plan.NewSubmissionIds
                            .Select(submissionId => SubmissionUser.Create(submissionId, projectId, documentTypeId, RoleConstants.ROLE_QA, plan.AssigneeUserId,
                                qaBatch.Id, currentUserId, now))
                            .ToList());
                    }
                    AddAuditLog(AuditLogConstants.ACTION_ASSIGNMENT_CREATE, AuditLogConstants.ACTION_TYPE_CREATE,
                        $"Giao QA {assigneeFullName} phụ trách {plan.NewOwnedNodes.Count} hộp / hồ sơ, {plan.NewSubmissionIds.Count} văn bản đã kiểm, phiếu #{ticket.Id}",
                        null, new { ticket.Id, ticket.Role, ticket.AssigneeUserId, plan.NewOwnedNodes, QaTicketId = qaBatchId, SubmissionIds = plan.NewSubmissionIds });
                }
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Assign)}, projectId={projectId}, documentTypeId={documentTypeId}, assigneeUserId={dto.AssigneeUserId}");
                await transaction.RollbackAsync();
                throw;
            }

            return await _assignmentQuery.FindTicketById(ticket.Id)
                ?? throw new UserFriendlyException(ErrorCodes.OperationErrorTicketNotFound);
        }

        public async Task<ViewAssignmentTicketDto> FindTicketById(int projectId, int documentTypeId, int ticketId)
        {
            _logger.LogInformation($"{nameof(FindTicketById)}, projectId={projectId}, documentTypeId={documentTypeId}, ticketId={ticketId}");
            await _projectMemberDomainService.EnsureCanManageAsync(projectId, documentTypeId, getCurrentUserId(), false);
            var ticket = await _assignmentQuery.FindTicketById(ticketId);
            if (ticket == null || ticket.ProjectId != projectId || ticket.DocumentTypeId != documentTypeId)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorTicketNotFound);
            }
            return ticket;
        }

        public async Task<BaseResponsePagingDto<GetListMyAssignmentDto>> FindMyAssignments(FindMyAssignmentDto dto)
        {
            _logger.LogInformation($"{nameof(FindMyAssignments)}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > AssignmentConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {AssignmentConst.MAX_PAGE_SIZE}");
            }
            if (dto.Kind != null)
            {
                AssignmentDomainService.EnsureAssignableRole(dto.Kind);
            }
            return await _assignmentQuery.FindMyAssignments(getCurrentUserId(), dto);
        }
    }
}
