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
using sh.be.domain.Module.Construction.QaAssignments.Repositories;
using sh.be.domain.Module.Construction.QaAssignments.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Core;
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
        private readonly IQaAssignmentQuery _qaAssignmentQuery;
        private readonly QaAssignmentDomainService _qaAssignmentDomainService;
        private readonly IQaStrategyValidator _qaStrategyValidator;

        public QaAssignmentService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<QaAssignmentService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IQaSamplingConfigRepository qaSamplingConfigRepository, IQaReviewerModeRepository qaReviewerModeRepository, IProjectRepository projectRepository,
            IQaAssignmentQuery qaAssignmentQuery, QaAssignmentDomainService qaAssignmentDomainService, IQaStrategyValidator qaStrategyValidator
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _qaSamplingConfigRepository = qaSamplingConfigRepository;
            _qaReviewerModeRepository = qaReviewerModeRepository;
            _projectRepository = projectRepository;
            _qaAssignmentQuery = qaAssignmentQuery;
            _qaAssignmentDomainService = qaAssignmentDomainService;
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
