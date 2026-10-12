using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces;
using sh.be.applications.Module.Construction.Dtos.DocumentField;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.Construction.Repositories;
using sh.be.domain.Module.Construction.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Construction.Implements
{
    public class DocumentFieldService : BaseService, IDocumentFieldService
    {
        private readonly IDocumentFieldRepository _documentFieldRepository;
        private readonly IDocumentFieldQuery _documentFieldQuery;
        private readonly DocumentFieldDomainService _documentFieldDomainService;

        public DocumentFieldService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<DocumentFieldService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IDocumentFieldRepository documentFieldRepository, IDocumentFieldQuery documentFieldQuery, DocumentFieldDomainService documentFieldDomainService
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _documentFieldRepository = documentFieldRepository;
            _documentFieldQuery = documentFieldQuery;
            _documentFieldDomainService = documentFieldDomainService;
        }

        public async Task<List<GetListDocumentFieldDropDownDto>> GetDocumentFieldDropDown(int documentTypeId)
        {
            _logger.LogInformation($"{nameof(GetDocumentFieldDropDown)}, documentTypeId={documentTypeId}");
            return await _documentFieldQuery.GetDocumentFieldDropDown(documentTypeId);
        }

        public async Task<BaseResponsePagingDto<GetListDocumentFieldDto>> FindPaging(FindPagingDocumentFieldDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto={JsonSerializer.Serialize(dto)}");
            return await _documentFieldQuery.FindPaging(dto);
        }

        public async Task<FindDocumentFieldByIdDto> FindById(int id)
        {
            _logger.LogInformation($"{nameof(FindById)}, id={id}");
            return await _documentFieldQuery.FindById(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentFieldNotFound);
        }

        public async Task Create(CreateDocumentFieldDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, dto={JsonSerializer.Serialize(dto)}");

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _documentFieldDomainService.LockOrderAsync(dto.DocumentTypeId);
                await _documentFieldDomainService.EnsureDocumentTypeExistsAsync(dto.DocumentTypeId);
                await _documentFieldDomainService.EnsureUniqueFieldAsync(dto.DocumentTypeId, dto.Field);
                var order = await _documentFieldDomainService.GetNextOrderAsync(dto.DocumentTypeId);

                var documentField = DocumentField.Create(dto.DocumentTypeId, dto.Name, dto.Field, dto.FieldImport, dto.IsShow,
                    dto.IsChargeable, dto.FieldNameOcr, dto.IsFieldOcr, order, getCurrentUserId(), GetVietnamTime());

                _documentFieldRepository.Add(documentField);
                AddAuditLog(AuditLogConstants.ACTION_DOCUMENT_FIELD_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Tạo trường {documentField.Name} của loại văn bản có Id = {documentField.DocumentTypeId}",
                    null, ToAuditData(documentField));
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Create)}, documentTypeId={dto.DocumentTypeId}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task QuickCreate(QuickCreateDocumentFieldDto dto)
        {
            _logger.LogInformation($"{nameof(QuickCreate)}, dto={JsonSerializer.Serialize(dto)}");
            var fieldNames = SplitFieldNames(dto.FieldNames);
            _documentFieldDomainService.EnsureHasFieldNames(fieldNames);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _documentFieldDomainService.LockOrderAsync(dto.DocumentTypeId);
                await _documentFieldDomainService.EnsureDocumentTypeExistsAsync(dto.DocumentTypeId);
                await _documentFieldDomainService.EnsureUniqueFieldsAsync(dto.DocumentTypeId, fieldNames);
                var nextOrder = await _documentFieldDomainService.GetNextOrderAsync(dto.DocumentTypeId);
                var currentUserId = getCurrentUserId();
                var now = GetVietnamTime();

                var documentFields = fieldNames
                    .Select((fieldName, index) => DocumentField.Create(dto.DocumentTypeId, fieldName, fieldName, fieldName, false,
                        false, null, false, nextOrder + index, currentUserId, now))
                    .ToList();

                _documentFieldRepository.AddRange(documentFields);
                AddAuditLog(AuditLogConstants.ACTION_DOCUMENT_FIELD_QUICK_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Thêm nhanh {documentFields.Count} trường của loại văn bản có Id = {dto.DocumentTypeId}",
                    null, documentFields.Select(ToAuditData).ToList());
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(QuickCreate)}, documentTypeId={dto.DocumentTypeId}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task Update(int id, UpdateDocumentFieldDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            var documentField = await _documentFieldRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentFieldNotFound);
            var oldData = ToAuditData(documentField);
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _documentFieldDomainService.LockOrderAsync(documentField.DocumentTypeId);
                await _documentFieldDomainService.EnsureUniqueFieldAsync(documentField.DocumentTypeId, dto.Field, id);
                documentField.Update(dto.Name, dto.Field, dto.FieldType, dto.FieldImport, dto.IsShow,
                    dto.IsShowTableView, dto.IsShowListView, dto.Width,
                    dto.IsChargeable, dto.FieldNameOcr, dto.IsFieldOcr, currentUserId, now);
                await _documentFieldDomainService.MoveToOrderAsync(documentField, dto.Order, currentUserId, now);

                _documentFieldRepository.Update(documentField);
                AddAuditLog(AuditLogConstants.ACTION_DOCUMENT_FIELD_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật trường {documentField.Name} của loại văn bản có Id = {documentField.DocumentTypeId}",
                    oldData, ToAuditData(documentField));
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Update)}, id={id}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task Delete(int id)
        {
            _logger.LogInformation($"{nameof(Delete)}, id={id}");
            var documentField = await _documentFieldRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentFieldNotFound);

            var oldData = ToAuditData(documentField);
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _documentFieldDomainService.LockOrderAsync(documentField.DocumentTypeId);
                documentField.Delete(currentUserId, now);

                _documentFieldRepository.Update(documentField);
                AddAuditLog(AuditLogConstants.ACTION_DOCUMENT_FIELD_DELETE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa trường {documentField.Name} của loại văn bản có Id = {documentField.DocumentTypeId}",
                    oldData, null);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Delete)}, id={id}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task QuickDelete(QuickDeleteDocumentFieldDto dto)
        {
            _logger.LogInformation($"{nameof(QuickDelete)}, dto={JsonSerializer.Serialize(dto)}");
            var ids = dto.Ids.Distinct().ToList();
            _documentFieldDomainService.EnsureHasIds(ids);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _documentFieldDomainService.LockOrderAsync(dto.DocumentTypeId);
                await _documentFieldDomainService.EnsureDocumentTypeExistsAsync(dto.DocumentTypeId);
                var documentFields = await _documentFieldRepository.FindByDocumentTypeIdAndIdsAsync(dto.DocumentTypeId, ids);
                _documentFieldDomainService.EnsureAllFound(ids, documentFields);
                var oldData = documentFields.Select(ToAuditData).ToList();
                var currentUserId = getCurrentUserId();
                var now = GetVietnamTime();

                foreach (var documentField in documentFields)
                {
                    documentField.Delete(currentUserId, now);
                    _documentFieldRepository.Update(documentField);
                }

                AddAuditLog(AuditLogConstants.ACTION_DOCUMENT_FIELD_QUICK_DELETE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa nhanh {documentFields.Count} trường của loại văn bản có Id = {dto.DocumentTypeId}",
                    oldData, null);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(QuickDelete)}, documentTypeId={dto.DocumentTypeId}, ids={string.Join(",", ids)}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static List<string> SplitFieldNames(string fieldNames)
        {
            return fieldNames
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static object ToAuditData(DocumentField documentField)
        {
            return new
            {
                documentField.Id,
                documentField.DocumentTypeId,
                documentField.Name,
                documentField.Field,
                documentField.FieldType,
                documentField.FieldImport,
                documentField.IsShow,
                documentField.OrderTableView,
                documentField.OrderListView,
                documentField.IsShowTableView,
                documentField.IsShowListView,
                documentField.Width,
                documentField.IsChargeable,
                documentField.FieldNameOcr,
                documentField.IsFieldOcr,
            };
        }
    }
}
