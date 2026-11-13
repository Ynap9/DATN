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

        // Danh sách phiếu của cặp lọc theo vai trò / người nhận / trạng thái; Leader chỉ thấy phiếu người mình quản lý (14.10)
        public async Task<BaseResponsePagingDto<ViewAssignmentTicketDto>> FindTicketPaging(int projectId, int documentTypeId, FindPagingAssignmentTicketDto dto)
        {
            _logger.LogInformation($"{nameof(FindTicketPaging)}, projectId={projectId}, documentTypeId={documentTypeId}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > AssignmentConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {AssignmentConst.MAX_PAGE_SIZE}");
            }
            if (!string.IsNullOrEmpty(dto.Role))
            {
                AssignmentDomainService.EnsureAssignableRole(dto.Role);
            }
            var access = await _projectMemberDomainService.EnsureCanManageAsync(projectId, documentTypeId, getCurrentUserId(), false);
            return await _assignmentQuery.FindTicketPaging(projectId, documentTypeId, dto, access.IsAdmin ? null : access.LeaderUserId);
        }

        // Thu hồi văn bản nhập liệu chưa kiểm của một người (theo phiếu hoặc theo hộp / hồ sơ), văn bản về "chưa giao"
        public async Task<AssignmentChangeResultDto> Revoke(int projectId, int documentTypeId, RevokeAssignmentDto dto)
        {
            _logger.LogInformation($"{nameof(Revoke)}, projectId={projectId}, documentTypeId={documentTypeId}, dto={JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var access = await _projectMemberDomainService.EnsureCanManageAsync(projectId, documentTypeId, currentUserId, true);
            var items = dto.Items?.Select(item => new AssignmentItem(item.Box, item.ProfileRange)).ToList();
            var now = GetVietnamTime();

            var result = new AssignmentChangeResultDto();
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _assignmentRepository.LockPairAsync(projectId, documentTypeId);
                var plan = await _assignmentDomainService.PlanRevokeAsync(access, dto.TicketId, dto.AssigneeUserId, items);
                var holderFullName = (await _assignmentQuery.FindFullNames([plan.HolderUserId])).GetValueOrDefault(plan.HolderUserId) ?? plan.HolderUserId;

                // Xoá có điều kiện chưa kiểm: văn bản vừa được kiểm giữa lúc lập kế hoạch và lúc xoá thì ở lại người cũ, phiếu chỉ giảm số xoá thật
                List<(AssignmentTicket Ticket, int RevokedCount, List<int> SubmissionIds)> revokedTickets = [];
                foreach (var ticket in plan.Tickets)
                {
                    var submissionIds = plan.Rows.Where(row => row.TicketId == ticket.Id).Select(row => row.SubmissionId).ToList();
                    var revokedCount = await _assignmentRepository.DeleteUnfinishedHoldersAsync(RoleConstants.ROLE_DATA, plan.HolderUserId, submissionIds);
                    if (revokedCount == 0)
                    {
                        continue;
                    }
                    ticket.DecreaseDocuments(revokedCount, currentUserId, now);
                    result.AffectedDocuments += revokedCount;
                    revokedTickets.Add((ticket, revokedCount, submissionIds));
                }
                if (result.AffectedDocuments == 0)
                {
                    throw new UserFriendlyException(ErrorCodes.OperationErrorNothingToRevoke);
                }
                await _unitOfWork.SaveChangesAsync();

                foreach (var (ticket, revokedCount, submissionIds) in revokedTickets)
                {
                    if (ticket.Status == AssignmentConst.STATUS_CANCELLED)
                    {
                        result.CancelledTicketIds.Add(ticket.Id);
                    }
                    else if (await _assignmentDomainService.TryMarkDoneAsync(ticket, access.Project.IsQaEnabled, now))
                    {
                        result.DoneTicketIds.Add(ticket.Id);
                    }
                    AddAuditLog(AuditLogConstants.ACTION_ASSIGNMENT_REVOKE, AuditLogConstants.ACTION_TYPE_DELETE,
                        $"Thu hồi {revokedCount} văn bản nhập liệu chưa kiểm của {holderFullName}, phiếu #{ticket.Id}",
                        new { ticket.Id, ticket.Role, ticket.AssigneeUserId, SelectedSubmissionIds = submissionIds },
                        new { RevokedCount = revokedCount, ticket.DocumentCount });
                }
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Revoke)}, projectId={projectId}, documentTypeId={documentTypeId}, ticketId={dto.TicketId}");
                await transaction.RollbackAsync();
                throw;
            }
            return result;
        }

        // Chuyển văn bản nhập liệu chưa kiểm của A sang phiếu mới của B; văn bản đã kiểm / đang bị trả ở lại A
        public async Task<AssignmentChangeResultDto> Transfer(int projectId, int documentTypeId, TransferAssignmentDto dto)
        {
            _logger.LogInformation($"{nameof(Transfer)}, projectId={projectId}, documentTypeId={documentTypeId}, dto={JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var access = await _projectMemberDomainService.EnsureCanManageAsync(projectId, documentTypeId, currentUserId, true);
            var items = dto.Items?.Select(item => new AssignmentItem(item.Box, item.ProfileRange)).ToList();
            var fullNames = await _assignmentQuery.FindFullNames([dto.FromUserId, dto.ToUserId]);
            var fromFullName = fullNames.GetValueOrDefault(dto.FromUserId) ?? dto.FromUserId;
            var toFullName = fullNames.GetValueOrDefault(dto.ToUserId) ?? dto.ToUserId;
            var now = GetVietnamTime();

            var result = new AssignmentChangeResultDto();
            AssignmentTicket newTicket;
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _assignmentRepository.LockPairAsync(projectId, documentTypeId);
                var plan = await _assignmentDomainService.PlanTransferAsync(access, dto.FromUserId, dto.ToUserId, dto.TicketId, items);

                newTicket = AssignmentTicket.CreateManual(projectId, documentTypeId, dto.ToUserId, RoleConstants.ROLE_DATA, plan.Rows.Count, dto.TicketId, currentUserId, now);
                _assignmentRepository.AddTicket(newTicket);
                await _unitOfWork.SaveChangesAsync();
                _assignmentRepository.AddTicketItems(plan.Rows
                    .DistinctBy(row => AssignmentDomainService.ToNodeKey(row.Box, row.ProfileRange))
                    .Select(row => AssignmentTicketItem.Create(newTicket.Id, row.Box, row.ProfileRange))
                    .ToList());

                // Xoá dòng của A có điều kiện chưa kiểm rồi mới tạo dòng của B (unique một người giữ / văn bản); lệch số = có văn bản vừa được kiểm → 812
                List<(AssignmentTicket Ticket, int TransferredCount)> sourceTickets = [];
                foreach (var ticket in plan.Tickets)
                {
                    var submissionIds = plan.Rows.Where(row => row.TicketId == ticket.Id).Select(row => row.SubmissionId).ToList();
                    var transferredCount = await _assignmentRepository.DeleteUnfinishedHoldersAsync(RoleConstants.ROLE_DATA, dto.FromUserId, submissionIds);
                    ticket.DecreaseDocuments(transferredCount, currentUserId, now);
                    result.AffectedDocuments += transferredCount;
                    sourceTickets.Add((ticket, transferredCount));
                }
                if (result.AffectedDocuments != plan.Rows.Count)
                {
                    throw new UserFriendlyException(ErrorCodes.OperationErrorAssignmentConflict);
                }
                await _assignmentRepository.AddHoldersAsync(plan.Rows
                    .Select(row => SubmissionUser.CreateEntry(row.SubmissionId, projectId, documentTypeId, dto.ToUserId, newTicket.Id, currentUserId, now))
                    .ToList());
                await _unitOfWork.SaveChangesAsync();

                foreach (var (ticket, transferredCount) in sourceTickets)
                {
                    if (ticket.Status == AssignmentConst.STATUS_CANCELLED)
                    {
                        result.CancelledTicketIds.Add(ticket.Id);
                    }
                    else if (await _assignmentDomainService.TryMarkDoneAsync(ticket, access.Project.IsQaEnabled, now))
                    {
                        result.DoneTicketIds.Add(ticket.Id);
                    }
                    AddAuditLog(AuditLogConstants.ACTION_ASSIGNMENT_TRANSFER, AuditLogConstants.ACTION_TYPE_UPDATE,
                        $"Chuyển {transferredCount} văn bản chưa kiểm từ {fromFullName} sang {toFullName}, phiếu #{ticket.Id} → #{newTicket.Id}",
                        new { ticket.Id, AssigneeUserId = dto.FromUserId, SubmissionIds = plan.Rows.Where(row => row.TicketId == ticket.Id).Select(row => row.SubmissionId) },
                        new { NewTicketId = newTicket.Id, AssigneeUserId = dto.ToUserId, TransferredCount = transferredCount, ticket.DocumentCount });
                }
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Transfer)}, projectId={projectId}, documentTypeId={documentTypeId}, fromUserId={dto.FromUserId}, toUserId={dto.ToUserId}");
                await transaction.RollbackAsync();
                throw;
            }

            result.NewTicket = await _assignmentQuery.FindTicketById(newTicket.Id);
            return result;
        }

        // Thu hồi phụ trách QA (cả phiếu hoặc một phần nút) cùng văn bản chưa đạt của QA đó trong các nút bị lấy, văn bản về "chưa giao"
        public async Task<AssignmentChangeResultDto> RevokeQaOwnership(int projectId, int documentTypeId, RevokeQaOwnershipDto dto)
        {
            _logger.LogInformation($"{nameof(RevokeQaOwnership)}, projectId={projectId}, documentTypeId={documentTypeId}, dto={JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var access = await _projectMemberDomainService.EnsureCanManageAsync(projectId, documentTypeId, currentUserId, true);
            var items = dto.Items?.Select(item => new AssignmentItem(item.Box, item.ProfileRange)).ToList();
            var now = GetVietnamTime();

            var result = new AssignmentChangeResultDto();
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _assignmentRepository.LockPairAsync(projectId, documentTypeId);
                var plan = await _assignmentDomainService.PlanQaRevokeAsync(access, dto.TicketId, items);
                var ownership = plan.Ownership;
                var holderFullName = (await _assignmentQuery.FindFullNames([ownership.AssigneeUserId])).GetValueOrDefault(ownership.AssigneeUserId) ?? ownership.AssigneeUserId;

                result.AffectedDocuments = await _assignmentDomainService.ReleaseQaOwnershipAsync(plan, currentUserId, now);
                if (ownership.Status == AssignmentConst.STATUS_CANCELLED)
                {
                    result.CancelledTicketIds.Add(ownership.Id);
                }
                AddAuditLog(AuditLogConstants.ACTION_ASSIGNMENT_REVOKE, AuditLogConstants.ACTION_TYPE_DELETE,
                    $"Thu hồi phụ trách QA {plan.RemovedItems.Count} nút, {result.AffectedDocuments} văn bản chưa đạt của {holderFullName}",
                    new { ownership.Id, ownership.Role, ownership.AssigneeUserId, RemovedItems = plan.RemovedItems.Select(item => new { item.Box, item.ProfileRange }),
                        SubmissionIds = plan.UnfinishedRows.Select(row => row.SubmissionId) },
                    new { RevokedCount = result.AffectedDocuments, plan.IsAllNodesRemoved });
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(RevokeQaOwnership)}, projectId={projectId}, documentTypeId={documentTypeId}, ticketId={dto.TicketId}");
                await transaction.RollbackAsync();
                throw;
            }
            return result;
        }

        // Chuyển phụ trách QA (cả phiếu hoặc một phần nút) từ Q1 sang Q2, văn bản chưa đạt vào đợt mở của Q2; văn bản đã đạt ở lại Q1
        public async Task<AssignmentChangeResultDto> TransferQaOwnership(int projectId, int documentTypeId, TransferQaOwnershipDto dto)
        {
            _logger.LogInformation($"{nameof(TransferQaOwnership)}, projectId={projectId}, documentTypeId={documentTypeId}, dto={JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var access = await _projectMemberDomainService.EnsureCanManageAsync(projectId, documentTypeId, currentUserId, true);
            var items = dto.Items?.Select(item => new AssignmentItem(item.Box, item.ProfileRange)).ToList();
            var now = GetVietnamTime();

            var result = new AssignmentChangeResultDto();
            AssignmentTicket targetOwnership;
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _assignmentRepository.LockPairAsync(projectId, documentTypeId);
                var plan = await _assignmentDomainService.PlanQaTransferAsync(access, dto.TicketId, dto.ToUserId, items);
                var sourceOwnership = plan.Ownership;
                var fullNames = await _assignmentQuery.FindFullNames([sourceOwnership.AssigneeUserId, dto.ToUserId]);
                var fromFullName = fullNames.GetValueOrDefault(sourceOwnership.AssigneeUserId) ?? sourceOwnership.AssigneeUserId;
                var toFullName = fullNames.GetValueOrDefault(dto.ToUserId) ?? dto.ToUserId;

                // Xoá dòng của Q1 trước khi tạo dòng của Q2 (unique một người giữ QA / văn bản); lệch số = có văn bản vừa đạt → 812
                result.AffectedDocuments = await _assignmentDomainService.ReleaseQaOwnershipAsync(plan, currentUserId, now);
                if (result.AffectedDocuments != plan.UnfinishedRows.Count)
                {
                    throw new UserFriendlyException(ErrorCodes.OperationErrorAssignmentConflict);
                }
                if (sourceOwnership.Status == AssignmentConst.STATUS_CANCELLED)
                {
                    result.CancelledTicketIds.Add(sourceOwnership.Id);
                }

                // Q2 đã phụ trách ở cặp thì gộp nút vào phiếu đó, chưa có thì tạo phụ trách mới trỏ về phụ trách gốc
                var existingOwnership = plan.TargetOwnershipTicketId == null ? null : await _assignmentRepository.FindTicketByIdAsync(plan.TargetOwnershipTicketId.Value);
                if (existingOwnership == null)
                {
                    targetOwnership = AssignmentTicket.CreateManual(projectId, documentTypeId, dto.ToUserId, RoleConstants.ROLE_QA,
                        result.AffectedDocuments, sourceOwnership.Id, currentUserId, now);
                    _assignmentRepository.AddTicket(targetOwnership);
                    await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    targetOwnership = existingOwnership;
                    targetOwnership.AddDocuments(result.AffectedDocuments, currentUserId, now);
                }
                var targetOwnershipId = targetOwnership.Id;
                _assignmentRepository.AddTicketItems(plan.RemovedItems
                    .Select(item => AssignmentTicketItem.Create(targetOwnershipId, item.Box, item.ProfileRange ?? ""))
                    .ToList());

                int? qaBatchId = null;
                if (result.AffectedDocuments > 0)
                {
                    var qaBatch = await _assignmentRepository.FindOpenQaBatchAsync(projectId, documentTypeId, dto.ToUserId, result.AffectedDocuments);
                    if (qaBatch == null)
                    {
                        qaBatch = QaTicket.CreateFull(projectId, documentTypeId, dto.ToUserId, result.AffectedDocuments, currentUserId, now);
                        _assignmentRepository.AddQaTicket(qaBatch);
                        await _unitOfWork.SaveChangesAsync();
                    }
                    else
                    {
                        qaBatch.AddDocuments(result.AffectedDocuments, currentUserId, now);
                    }
                    qaBatchId = qaBatch.Id;
                    await _assignmentRepository.AddHoldersAsync(plan.UnfinishedRows
                        .Select(row => SubmissionUser.Create(row.SubmissionId, projectId, documentTypeId, RoleConstants.ROLE_QA, dto.ToUserId,
                            qaBatch.Id, currentUserId, now))
                        .ToList());
                }
                AddAuditLog(AuditLogConstants.ACTION_ASSIGNMENT_TRANSFER, AuditLogConstants.ACTION_TYPE_UPDATE,
                    $"Chuyển phụ trách QA {plan.RemovedItems.Count} nút, {result.AffectedDocuments} văn bản chưa đạt từ {fromFullName} sang {toFullName}",
                    new { sourceOwnership.Id, sourceOwnership.AssigneeUserId, RemovedItems = plan.RemovedItems.Select(item => new { item.Box, item.ProfileRange }),
                        SubmissionIds = plan.UnfinishedRows.Select(row => row.SubmissionId) },
                    new { OwnershipTicketId = targetOwnershipId, AssigneeUserId = dto.ToUserId, QaTicketId = qaBatchId, TransferredCount = result.AffectedDocuments });
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(TransferQaOwnership)}, projectId={projectId}, documentTypeId={documentTypeId}, ticketId={dto.TicketId}, toUserId={dto.ToUserId}");
                await transaction.RollbackAsync();
                throw;
            }

            result.NewTicket = await _assignmentQuery.FindTicketById(targetOwnership.Id);
            return result;
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
