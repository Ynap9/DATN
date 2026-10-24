using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces.External.DataReader;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Module.Construction.Dtos.DocumentField;
using sh.be.applications.Module.Construction.Dtos.DocumentType;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Construction.DocumentTypes.Entities;
using sh.be.domain.Module.Construction.DocumentTypes.Repositories;
using sh.be.domain.Module.Construction.DocumentTypes.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Construction.Implements
{
    public class DocumentTypeService : BaseService, IDocumentTypeService
    {
        private readonly IDocumentTypeRepository _documentTypeRepository;
        private readonly IDocumentTypeQuery _documentTypeQuery;
        private readonly DocumentTypeDomainService _documentTypeDomainService;
        private readonly IExcelExternal _excelExternal;

        public DocumentTypeService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<DocumentTypeService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IDocumentTypeRepository documentTypeRepository, IDocumentTypeQuery documentTypeQuery, DocumentTypeDomainService documentTypeDomainService, IExcelExternal excelExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _documentTypeRepository = documentTypeRepository;
            _documentTypeQuery = documentTypeQuery;
            _documentTypeDomainService = documentTypeDomainService;
            _excelExternal = excelExternal;
        }

        public async Task<List<GetListDocumentTypeDropDownDto>> GetDocumentTypeDropDown()
        {
            _logger.LogInformation($"{nameof(GetDocumentTypeDropDown)}");
            return await _documentTypeQuery.GetDocumentTypeDropDown();
        }

        public async Task<BaseResponsePagingDto<GetListDocumentTypeDto>> FindPaging(FindPagingDocumentTypeDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto={JsonSerializer.Serialize(dto)}");
            return await _documentTypeQuery.FindPaging(dto);
        }

        public async Task<FindDocumentTypeByIdDto> FindById(int id)
        {
            _logger.LogInformation($"{nameof(FindById)}, id={id}");
            return await _documentTypeQuery.FindById(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentTypeNotFound);
        }

        public async Task<List<GetListDocumentFieldDto>> GetListDocumentField(int id)
        {
            _logger.LogInformation($"{nameof(GetListDocumentField)}, id={id}");
            _ = await _documentTypeRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentTypeNotFound);
            return await _documentTypeQuery.GetListDocumentField(id);
        }

        public async Task<ExportFileDto> ExportExcelTemplate(int id)
        {
            _logger.LogInformation($"{nameof(ExportExcelTemplate)}, id={id}");
            var documentType = await _documentTypeQuery.FindById(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentTypeNotFound);
            var headers = await _documentTypeQuery.GetListTemplateHeader(id);
            return _excelExternal.CreateTemplate(documentType.Name, headers);
        }

        public async Task Create(CreateDocumentTypeDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, dto={JsonSerializer.Serialize(dto)}");
            await _documentTypeDomainService.EnsureUniqueNameAsync(dto.Name);

            var documentType = DocumentType.Create(dto.Name, getCurrentUserId(), GetVietnamTime(), dto.DefaultView);

            _documentTypeRepository.Add(documentType);
            AddAuditLog(AuditLogConstants.ACTION_DOCUMENT_TYPE_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Tạo loại văn bản {documentType.Name}",
                null, new { documentType.Name, documentType.DefaultView });
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task Update(int id, UpdateDocumentTypeDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            var documentType = await _documentTypeRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentTypeNotFound);
            var oldData = new { documentType.Id, documentType.Name, documentType.DefaultView };

            await _documentTypeDomainService.EnsureUniqueNameAsync(dto.Name, id);
            documentType.Update(dto.Name, dto.DefaultView, getCurrentUserId(), GetVietnamTime());

            _documentTypeRepository.Update(documentType);
            AddAuditLog(AuditLogConstants.ACTION_DOCUMENT_TYPE_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật loại văn bản {documentType.Name}",
                oldData, new { documentType.Id, documentType.Name, documentType.DefaultView });
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            _logger.LogInformation($"{nameof(Delete)}, id={id}");
            var documentType = await _documentTypeRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentTypeNotFound);

            documentType.Delete(getCurrentUserId(), GetVietnamTime());

            _documentTypeRepository.Update(documentType);
            AddAuditLog(AuditLogConstants.ACTION_DOCUMENT_TYPE_DELETE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa loại văn bản {documentType.Name}",
                new { documentType.Id, documentType.Name, documentType.DefaultView }, null);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
