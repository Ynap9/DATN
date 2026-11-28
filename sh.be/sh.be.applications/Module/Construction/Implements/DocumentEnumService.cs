using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Module.Construction.Dtos.DocumentEnum;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Auth.Services;
using sh.be.domain.Module.Construction.DocumentFields.Entities;
using sh.be.domain.Module.Construction.DocumentFields.Repositories;
using sh.be.domain.Module.Construction.DocumentFields.Services;
using sh.be.domain.Module.Construction.DocumentTypes.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Core;
using System.Text.Json;

namespace sh.be.applications.Module.Construction.Implements
{
    public class DocumentEnumService : BaseService, IDocumentEnumService
    {
        private readonly IDocumentFieldRepository _documentFieldRepository;
        private readonly IDocumentEnumQuery _documentEnumQuery;
        private readonly IRoleRepository _roleRepository;
        private readonly DocumentFieldDomainService _documentFieldDomainService;
        private readonly DocumentTypeDomainService _documentTypeDomainService;

        public DocumentEnumService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<DocumentEnumService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IDocumentFieldRepository documentFieldRepository, IDocumentEnumQuery documentEnumQuery, IRoleRepository roleRepository,
            DocumentFieldDomainService documentFieldDomainService, DocumentTypeDomainService documentTypeDomainService
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _documentFieldRepository = documentFieldRepository;
            _documentEnumQuery = documentEnumQuery;
            _roleRepository = roleRepository;
            _documentFieldDomainService = documentFieldDomainService;
            _documentTypeDomainService = documentTypeDomainService;
        }

        // Danh mục của loại văn bản kèm các trường đang dùng; ngoài phạm vi xem trả 301 như API document-fields
        public async Task<List<GetListDocumentEnumDto>> GetList(int documentTypeId)
        {
            _logger.LogInformation($"{nameof(GetList)}, documentTypeId={documentTypeId}");
            await _documentFieldDomainService.EnsureDocumentTypeExistsAsync(documentTypeId);
            var currentUserId = getCurrentUserId();
            var isAdmin = PermissionDomainService.IsAdmin(await _roleRepository.GetRoleNamesByUserIdAsync(currentUserId));
            await _documentTypeDomainService.EnsureViewableAsync(documentTypeId, currentUserId, isAdmin);
            return await _documentEnumQuery.GetList(documentTypeId);
        }

        // Tạo danh mục: khoá loại văn bản để tên không trùng khi tạo song song
        public async Task<int> Create(int documentTypeId, CreateDocumentEnumDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, documentTypeId={documentTypeId}, dto={JsonSerializer.Serialize(dto)}");
            var name = DocumentFieldDomainService.NormalizeEnumName(dto.Name);
            var values = DocumentFieldDomainService.NormalizeEnumValues(dto.Values);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _documentFieldDomainService.LockOrderAsync(documentTypeId);
                await _documentFieldDomainService.EnsureDocumentTypeExistsAsync(documentTypeId);
                await _documentFieldDomainService.EnsureUniqueEnumNameAsync(documentTypeId, name);

                var documentEnum = DocumentEnum.Create(documentTypeId, name, DocumentFieldDomainService.SerializeEnumValues(values), getCurrentUserId(), GetVietnamTime());

                _documentFieldRepository.AddEnum(documentEnum);
                await _unitOfWork.SaveChangesAsync();
                AddAuditLog(AuditLogConstants.ACTION_DOCUMENT_ENUM_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Tạo danh mục {documentEnum.Name} của loại văn bản có Id = {documentTypeId}",
                    null, ToAuditData(documentEnum));
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
                return documentEnum.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Create)}, documentTypeId={documentTypeId}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Sửa tên / giá trị danh mục; dữ liệu văn bản đã nhập không đổi
        public async Task Update(int documentTypeId, int enumId, UpdateDocumentEnumDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, documentTypeId={documentTypeId}, enumId={enumId}, dto={JsonSerializer.Serialize(dto)}");
            var name = DocumentFieldDomainService.NormalizeEnumName(dto.Name);
            var values = DocumentFieldDomainService.NormalizeEnumValues(dto.Values);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _documentFieldDomainService.LockOrderAsync(documentTypeId);
                await _documentFieldDomainService.EnsureDocumentTypeExistsAsync(documentTypeId);
                var documentEnum = await _documentFieldDomainService.FindEnumAsync(documentTypeId, enumId);
                var oldData = ToAuditData(documentEnum);
                await _documentFieldDomainService.EnsureUniqueEnumNameAsync(documentTypeId, name, enumId);

                documentEnum.Update(name, DocumentFieldDomainService.SerializeEnumValues(values), getCurrentUserId(), GetVietnamTime());

                _documentFieldRepository.UpdateEnum(documentEnum);
                AddAuditLog(AuditLogConstants.ACTION_DOCUMENT_ENUM_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật danh mục {documentEnum.Name} của loại văn bản có Id = {documentTypeId}",
                    oldData, ToAuditData(documentEnum));
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Update)}, documentTypeId={documentTypeId}, enumId={enumId}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Xoá mềm danh mục; còn trường chưa xoá trỏ tới thì trả 350 kèm tên trường
        public async Task Delete(int documentTypeId, int enumId)
        {
            _logger.LogInformation($"{nameof(Delete)}, documentTypeId={documentTypeId}, enumId={enumId}");

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _documentFieldDomainService.LockOrderAsync(documentTypeId);
                await _documentFieldDomainService.EnsureDocumentTypeExistsAsync(documentTypeId);
                var documentEnum = await _documentFieldDomainService.FindEnumAsync(documentTypeId, enumId);
                await _documentFieldDomainService.EnsureEnumNotUsedAsync(enumId);

                documentEnum.Delete(getCurrentUserId(), GetVietnamTime());

                _documentFieldRepository.UpdateEnum(documentEnum);
                AddAuditLog(AuditLogConstants.ACTION_DOCUMENT_ENUM_DELETE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa danh mục {documentEnum.Name} của loại văn bản có Id = {documentTypeId}",
                    ToAuditData(documentEnum), null);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Delete)}, documentTypeId={documentTypeId}, enumId={enumId}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static object ToAuditData(DocumentEnum documentEnum)
        {
            return new
            {
                documentEnum.Id,
                documentEnum.DocumentTypeId,
                documentEnum.Name,
                Values = DocumentFieldDomainService.ParseEnumValues(documentEnum.ValuesJson),
            };
        }
    }
}
