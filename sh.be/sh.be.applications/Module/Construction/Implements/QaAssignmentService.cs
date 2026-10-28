using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Module.Construction.Dtos.QaAssignment;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Construction.Projects.Repositories;
using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.QaAssignments.Models;
using sh.be.domain.Module.Construction.QaAssignments.Repositories;
using sh.be.domain.Module.Construction.QaAssignments.Services;
using sh.be.domain.Module.Construction.Submissions.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Construction.Implements
{
    public class QaAssignmentService : BaseService, IQaAssignmentService
    {
        private readonly IQaSamplingConfigRepository _qaSamplingConfigRepository;
        private readonly IQaReviewerModeRepository _qaReviewerModeRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IQaTicketRepository _qaTicketRepository;
        private readonly IQaAssignmentQuery _qaAssignmentQuery;
        private readonly QaAssignmentDomainService _qaAssignmentDomainService;
        private readonly SubmissionDomainService _submissionDomainService;
        private readonly IQaStrategyValidator _qaStrategyValidator;

        public QaAssignmentService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<QaAssignmentService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IQaSamplingConfigRepository qaSamplingConfigRepository, IQaReviewerModeRepository qaReviewerModeRepository, IProjectRepository projectRepository,
            IQaTicketRepository qaTicketRepository, IQaAssignmentQuery qaAssignmentQuery, QaAssignmentDomainService qaAssignmentDomainService,
            SubmissionDomainService submissionDomainService, IQaStrategyValidator qaStrategyValidator
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _qaSamplingConfigRepository = qaSamplingConfigRepository;
            _qaReviewerModeRepository = qaReviewerModeRepository;
            _projectRepository = projectRepository;
            _qaTicketRepository = qaTicketRepository;
            _qaAssignmentQuery = qaAssignmentQuery;
            _qaAssignmentDomainService = qaAssignmentDomainService;
            _submissionDomainService = submissionDomainService;
            _qaStrategyValidator = qaStrategyValidator;
        }

        public async Task<ViewQaSamplingConfigDto> FindConfig(int projectId)
        {
            _logger.LogInformation($"{nameof(FindConfig)}, projectId={projectId}");
            _ = await _projectRepository.FindByIdAsync(projectId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
            var config = await _qaAssignmentDomainService.GetConfigOrDefaultAsync(projectId);
            return _mapper.Map<ViewQaSamplingConfigDto>(config);
        }

        public async Task UpdateConfig(UpdateQaSamplingConfigDto dto)
        {
            _logger.LogInformation($"{nameof(UpdateConfig)}, dto={JsonSerializer.Serialize(dto)}");
            var project = await _projectRepository.FindByIdAsync(dto.ProjectId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
            var strategyParamsJson = string.IsNullOrWhiteSpace(dto.StrategyParamsJson) ? null : dto.StrategyParamsJson;
            _qaStrategyValidator.Validate(dto.StrategyCode, strategyParamsJson);

            var now = GetVietnamTime();
            var config = await _qaSamplingConfigRepository.FindByProjectAsync(dto.ProjectId);
            if (config == null)
            {
                config = QaSamplingConfig.Create(dto.ProjectId, dto.SampleSize, dto.ThresholdPercent, dto.StrategyCode, strategyParamsJson,
                    dto.StopEarlyOnReject, getCurrentUserId(), now);
                _qaSamplingConfigRepository.Add(config);
                AddAuditLog(AuditLogConstants.ACTION_QA_SAMPLING_CONFIG_UPDATE, AuditLogConstants.ACTION_TYPE_CREATE,
                    $"Tạo cấu hình chấm mẫu dự án {project.Name}", null, ToAuditData(config));
            }
            else
            {
                var oldData = ToAuditData(config);
                config.Update(dto.SampleSize, dto.ThresholdPercent, dto.StrategyCode, strategyParamsJson, dto.StopEarlyOnReject, getCurrentUserId(), now);
                _qaSamplingConfigRepository.Update(config);
                AddAuditLog(AuditLogConstants.ACTION_QA_SAMPLING_CONFIG_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE,
                    $"Cập nhật cấu hình chấm mẫu dự án {project.Name}", oldData, ToAuditData(config));
            }
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<GetListQaReviewerModeDto>> FindReviewerModes(int projectId)
        {
            _logger.LogInformation($"{nameof(FindReviewerModes)}, projectId={projectId}");
            _ = await _projectRepository.FindByIdAsync(projectId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
            return await _qaAssignmentQuery.FindReviewerModes(projectId);
        }

        public async Task UpdateReviewerMode(UpdateQaReviewerModeDto dto)
        {
            _logger.LogInformation($"{nameof(UpdateReviewerMode)}, dto={JsonSerializer.Serialize(dto)}");
            var project = await _projectRepository.FindByIdAsync(dto.ProjectId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
            var user = await _qaAssignmentDomainService.EnsureQaReviewerAsync(dto.UserId);

            var now = GetVietnamTime();
            var reviewerMode = await _qaReviewerModeRepository.FindAsync(dto.ProjectId, user.Id);
            var oldData = reviewerMode == null ? null : new { reviewerMode.ProjectId, reviewerMode.UserId, reviewerMode.Mode };
            if (reviewerMode == null)
            {
                reviewerMode = QaReviewerMode.Create(dto.ProjectId, user.Id, dto.Mode, getCurrentUserId(), now);
                _qaReviewerModeRepository.Add(reviewerMode);
            }
            else
            {
                reviewerMode.ChangeMode(dto.Mode, getCurrentUserId(), now);
                _qaReviewerModeRepository.Update(reviewerMode);
            }
            AddAuditLog(AuditLogConstants.ACTION_QA_REVIEWER_MODE_UPDATE, oldData == null ? AuditLogConstants.ACTION_TYPE_CREATE : AuditLogConstants.ACTION_TYPE_UPDATE,
                $"Đặt chế độ {reviewerMode.Mode} cho {user.UserName} ở dự án {project.Name}", oldData,
                new { reviewerMode.ProjectId, reviewerMode.UserId, reviewerMode.Mode });
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<FindQaTicketByIdDto> CreateTicket(CreateQaTicketDto dto)
        {
            _logger.LogInformation($"{nameof(CreateTicket)}, dto={JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            var access = await _qaAssignmentDomainService.GetAccessAsync(currentUserId);
            _qaAssignmentDomainService.EnsureManagesPair(access, dto.ProjectId, dto.DocumentTypeId);
            var project = await _submissionDomainService.EnsureEditableProjectAsync(dto.ProjectId, access.IsAdmin);
            var qaUser = await _qaAssignmentDomainService.EnsureQaReviewerAsync(dto.QaUserId);
            await _qaAssignmentDomainService.EnsureQaMemberAsync(access, currentUserId, dto.ProjectId, dto.DocumentTypeId, qaUser.Id);

            var nodes = dto.Items?
                .Select(item => new QaTicketNode(item.Box.Trim(), string.IsNullOrWhiteSpace(item.ProfileRange) ? null : item.ProfileRange.Trim()))
                .ToList();
            var candidates = await _qaAssignmentDomainService.SelectAssignableAsync(dto.ProjectId, dto.DocumentTypeId, qaUser.Id, nodes, dto.Count);
            await _qaAssignmentDomainService.EnsureAssignableModeAsync(dto.ProjectId, qaUser.Id);

            var now = GetVietnamTime();
            var ticket = QaTicket.CreateFull(dto.ProjectId, dto.DocumentTypeId, qaUser.Id, candidates.Count, currentUserId, now);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                _qaTicketRepository.Add(ticket);
                await _unitOfWork.SaveChangesAsync();

                await _qaTicketRepository.AddQaAssignmentsAsync(ticket.AssignSubmissions(candidates, currentUserId, now));
                AddAuditLog(AuditLogConstants.ACTION_QA_TICKET_CREATE, AuditLogConstants.ACTION_TYPE_CREATE,
                    $"Giao {candidates.Count} văn bản cho QA {qaUser.UserName}, chế độ {ticket.Mode}, phiếu #{ticket.Id} (dự án {project.Name})", null,
                    new
                    {
                        ticket.Id,
                        ticket.ProjectId,
                        ticket.DocumentTypeId,
                        ticket.QaUserId,
                        ticket.Mode,
                        ticket.DocumentCount,
                        SubmissionIds = candidates.Select(candidate => candidate.SubmissionId),
                    });
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(CreateTicket)}, projectId={dto.ProjectId}, documentTypeId={dto.DocumentTypeId}, qaUserId={dto.QaUserId}");
                await transaction.RollbackAsync();
                throw;
            }

            return await _qaAssignmentQuery.FindTicketById(ticket.Id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorQaTicketNotFound);
        }

        public async Task<BaseResponsePagingDto<GetListQaTicketDto>> FindTicketPaging(FindPagingQaTicketDto dto)
        {
            _logger.LogInformation($"{nameof(FindTicketPaging)}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > SubmissionConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {SubmissionConst.MAX_PAGE_SIZE}");
            }
            var currentUserId = getCurrentUserId();
            var access = await _qaAssignmentDomainService.GetAccessAsync(currentUserId);
            return await _qaAssignmentQuery.FindTicketPaging(dto, access.IsAdmin ? null : currentUserId);
        }

        public async Task<FindQaTicketByIdDto> FindTicketById(int id)
        {
            _logger.LogInformation($"{nameof(FindTicketById)}, id={id}");
            await _qaAssignmentDomainService.EnsureVisibleTicketAsync(id, getCurrentUserId());
            return await _qaAssignmentQuery.FindTicketById(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorQaTicketNotFound);
        }

        public async Task<FindQaTicketByIdDto> CancelTicket(int id)
        {
            _logger.LogInformation($"{nameof(CancelTicket)}, id={id}");
            var currentUserId = getCurrentUserId();
            var access = await _qaAssignmentDomainService.GetAccessAsync(currentUserId);
            var ticket = await _qaAssignmentDomainService.EnsureVisibleTicketAsync(id, currentUserId, access);
            var project = await _submissionDomainService.EnsureEditableProjectAsync(ticket.ProjectId, access.IsAdmin);
            var oldStatus = ticket.Status;

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _qaAssignmentDomainService.CancelTicketAsync(ticket, currentUserId, GetVietnamTime());
                var releasedCount = await _qaTicketRepository.ReleaseNotPassedAsync(ticket.Id);
                AddAuditLog(AuditLogConstants.ACTION_QA_TICKET_CANCEL, AuditLogConstants.ACTION_TYPE_UPDATE,
                    $"Huỷ phiếu #{ticket.Id} (dự án {project.Name}), trả {releasedCount} văn bản chưa QA Đạt về chưa giao",
                    new { ticket.Id, Status = oldStatus }, new { ticket.Id, ticket.Status, ReleasedCount = releasedCount });
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(CancelTicket)}, id={id}");
                await transaction.RollbackAsync();
                throw;
            }

            return await _qaAssignmentQuery.FindTicketById(ticket.Id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorQaTicketNotFound);
        }

        private static object ToAuditData(QaSamplingConfig config)
        {
            return new
            {
                config.ProjectId,
                config.SampleSize,
                config.ThresholdPercent,
                config.StrategyCode,
                config.StrategyParamsJson,
                config.StopEarlyOnReject,
            };
        }
    }
}
