using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.DataReader;
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
        private readonly IDataReader _dataReader;

        public DocumentFieldService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<DocumentFieldService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IDocumentFieldRepository documentFieldRepository, IDocumentFieldQuery documentFieldQuery, DocumentFieldDomainService documentFieldDomainService, IDataReader dataReader
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _documentFieldRepository = documentFieldRepository;
            _documentFieldQuery = documentFieldQuery;
            _documentFieldDomainService = documentFieldDomainService;
            _dataReader = dataReader;
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

        public async Task<List<SheetHeaderDto>> GetSheetHeaders(GetSheetHeaderDto dto)
        {
            _logger.LogInformation($"{nameof(GetSheetHeaders)}, dto={JsonSerializer.Serialize(dto)}");
            return await _dataReader.GetSheetHeadersAsync(dto.Url);
        }

        public async Task<List<FieldImportHeaderDto>> MapFieldImport(MapFieldImportDto dto)
        {
            _logger.LogInformation($"{nameof(MapFieldImport)}, dto={JsonSerializer.Serialize(dto)}");
            await _documentFieldDomainService.EnsureDocumentTypeExistsAsync(dto.DocumentTypeId);
            var headers = await _dataReader.GetHeadersAsync(dto.Url, dto.SheetName, dto.HeaderStartIndex);
            var fieldImports = await _documentFieldQuery.GetListFieldImport(dto.DocumentTypeId);

            foreach (var fieldImport in fieldImports)
            {
                fieldImport.Headers = headers
                    .Where(header => FieldImportMatchPolicy.IsMatch(fieldImport.FieldImport, header))
                    .ToList();
            }
            return fieldImports;
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

        public async Task UpdateSetting(int id, UpdateSettingDocumentFieldDto dto)
        {
            _logger.LogInformation($"{nameof(UpdateSetting)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            var documentField = await _documentFieldRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentFieldNotFound);
            var oldData = ToAuditData(documentField);

            documentField.UpdateSetting(dto.IsShow, dto.IsChargeable, dto.IsFieldOcr, dto.FieldNameOcr,
                getCurrentUserId(), GetVietnamTime());

            _documentFieldRepository.Update(documentField);
            AddAuditLog(AuditLogConstants.ACTION_DOCUMENT_FIELD_UPDATE_SETTING, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật cấu hình trường {documentField.Name} của loại văn bản có Id = {documentField.DocumentTypeId}",
                oldData, ToAuditData(documentField));
            await _unitOfWork.SaveChangesAsync();
        }

        public Task ChangeOrderTableView(int id, ChangeOrderDocumentFieldDto dto)
        {
            _logger.LogInformation($"{nameof(ChangeOrderTableView)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            return ChangeOrderAsync(id, AuditLogConstants.ACTION_DOCUMENT_FIELD_CHANGE_ORDER_TABLE_VIEW, "thứ tự hiển thị dạng bảng",
                (documentField, modifiedBy, modifiedDate) => _documentFieldDomainService.MoveOrderTableViewAsync(documentField, dto.TargetOrder, modifiedBy, modifiedDate));
        }

        public Task ChangeOrderListView(int id, ChangeOrderDocumentFieldDto dto)
        {
            _logger.LogInformation($"{nameof(ChangeOrderListView)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            return ChangeOrderAsync(id, AuditLogConstants.ACTION_DOCUMENT_FIELD_CHANGE_ORDER_LIST_VIEW, "thứ tự hiển thị dạng danh sách",
                (documentField, modifiedBy, modifiedDate) => _documentFieldDomainService.MoveOrderListViewAsync(documentField, dto.TargetOrder, modifiedBy, modifiedDate));
        }

        private async Task ChangeOrderAsync(int id, string action, string orderName, Func<DocumentField, string?, DateTime, Task> moveOrder)
        {
            var documentTypeId = await _documentFieldRepository.FindDocumentTypeIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentFieldNotFound);
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _documentFieldDomainService.LockOrderAsync(documentTypeId);
                var documentField = await _documentFieldRepository.FindByIdAsync(id)
                    ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentFieldNotFound);
                var oldData = ToAuditData(documentField);

                await moveOrder(documentField, currentUserId, now);

                _documentFieldRepository.Update(documentField);
                AddAuditLog(action, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật {orderName} của trường {documentField.Name} của loại văn bản có Id = {documentField.DocumentTypeId}",
                    oldData, ToAuditData(documentField));
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(ChangeOrderAsync)}, id={id}");
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
