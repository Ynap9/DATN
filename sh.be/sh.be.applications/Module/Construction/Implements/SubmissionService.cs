using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces;
using sh.be.applications.Module.Construction.Dtos.Submission;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Construction.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Construction.Implements
{
    public class SubmissionService : BaseService, ISubmissionService
    {
        private readonly ISubmissionQuery _submissionQuery;
        private readonly SubmissionDomainService _submissionDomainService;

        public SubmissionService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<SubmissionService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            ISubmissionQuery submissionQuery, SubmissionDomainService submissionDomainService
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _submissionQuery = submissionQuery;
            _submissionDomainService = submissionDomainService;
        }

        public async Task<BaseResponsePagingDto<GetListSubmissionDto>> FindPaging(FindPagingSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > SubmissionConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {SubmissionConst.MAX_PAGE_SIZE}");
            }
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            return await _submissionQuery.FindPaging(dto);
        }

        public async Task<ViewSubmissionDetailDto> FindById(int id, FindSubmissionDetailDto dto)
        {
            _logger.LogInformation($"{nameof(FindById)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            var detail = await _submissionQuery.FindById(id, dto, dto.IncludePosition)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotFound);
            EnsureSubmissionInFilter(detail, dto);
            return detail;
        }

        public async Task<ViewSubmissionDetailDto> FindNeighbor(int id, FindSubmissionNeighborDto dto)
        {
            _logger.LogInformation($"{nameof(FindNeighbor)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.Direction != SubmissionConst.NEIGHBOR_NEXT && dto.Direction != SubmissionConst.NEIGHBOR_PREVIOUS)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"direction chỉ nhận {SubmissionConst.NEIGHBOR_NEXT} hoặc {SubmissionConst.NEIGHBOR_PREVIOUS}");
            }
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            var neighborId = await _submissionQuery.FindNeighborId(id, dto, dto.Direction)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionPositionOutOfRange);
            return await _submissionQuery.FindById(neighborId, dto, false)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotFound);
        }

        public async Task<ViewCountSubmissionDto> Count(SubmissionFilterDto dto)
        {
            _logger.LogInformation($"{nameof(Count)}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            return await _submissionQuery.Count(dto);
        }

        private static void EnsureSubmissionInFilter(ViewSubmissionDetailDto detail, ISubmissionFilter filter)
        {
            if (detail.ProjectId != filter.ProjectId || detail.DocumentTypeId != filter.DocumentTypeId)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotInDocumentType);
            }
        }
    }
}
