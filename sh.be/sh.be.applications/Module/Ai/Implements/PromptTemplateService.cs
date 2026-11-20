using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Module.Ai.Dtos.PromptTemplate;
using sh.be.applications.Module.Ai.Interfaces.Queries;
using sh.be.applications.Module.Ai.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Ai.Prompts.Repositories;
using sh.be.domain.Module.Ai.Prompts.Services;
using sh.be.domain.Module.Construction.DocumentFields.Repositories;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Ai.Implements
{
    public class PromptTemplateService : BaseService, IPromptTemplateService
    {
        private readonly IPromptTemplateRepository _promptTemplateRepository;
        private readonly IPromptFieldConfigRepository _promptFieldConfigRepository;
        private readonly IDocumentFieldRepository _documentFieldRepository;
        private readonly IPromptTemplateQuery _promptTemplateQuery;
        private readonly PromptTemplateDomainService _promptTemplateDomainService;

        public PromptTemplateService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<PromptTemplateService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IPromptTemplateRepository promptTemplateRepository, IPromptFieldConfigRepository promptFieldConfigRepository, IDocumentFieldRepository documentFieldRepository,
            IPromptTemplateQuery promptTemplateQuery, PromptTemplateDomainService promptTemplateDomainService
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _promptTemplateRepository = promptTemplateRepository;
            _promptFieldConfigRepository = promptFieldConfigRepository;
            _documentFieldRepository = documentFieldRepository;
            _promptTemplateQuery = promptTemplateQuery;
            _promptTemplateDomainService = promptTemplateDomainService;
        }

        // Màn sửa prompt của metadata kèm 2 danh sách cảnh báo trường thiếu / không còn hợp lệ (21.8)
        public async Task<ViewPromptEditorDto> FindEditor(int documentTypeId)
        {
            _logger.LogInformation($"{nameof(FindEditor)}, documentTypeId={documentTypeId}");
            await _promptTemplateDomainService.EnsureDocumentTypeExistsAsync(documentTypeId);
            var editor = await _promptTemplateQuery.FindEditor(documentTypeId);

            var configuredFieldIds = editor.EditingId.HasValue
                ? (await _promptFieldConfigRepository.GetByTemplateIdAsync(editor.EditingId.Value)).Select(config => config.DocumentFieldId).ToList()
                : [];
            var classification = PromptTemplateDomainService.Classify(configuredFieldIds, await _documentFieldRepository.GetByDocumentTypeIdAsync(documentTypeId));
            editor.MissingFields = await _promptTemplateQuery.GetDocumentFieldNames(classification.MissingFieldIds);
            editor.InvalidFields = await _promptTemplateQuery.GetDocumentFieldNames(classification.InvalidFieldIds);
            return editor;
        }

        // Danh sách phiên bản prompt của metadata, mới nhất trước
        public async Task<List<GetListPromptTemplateVersionDto>> FindVersions(int documentTypeId)
        {
            _logger.LogInformation($"{nameof(FindVersions)}, documentTypeId={documentTypeId}");
            await _promptTemplateDomainService.EnsureDocumentTypeExistsAsync(documentTypeId);
            return await _promptTemplateQuery.FindVersions(documentTypeId);
        }

        // Lưu nháp: sửa nháp hiện có hoặc tạo nháp mới, đồng bộ cấu hình trường theo DocumentFieldId (xoá trước, rồi sửa / thêm)
        public async Task<int> SaveDraft(int documentTypeId, SavePromptDraftDto dto)
        {
            _logger.LogInformation($"{nameof(SaveDraft)}, documentTypeId={documentTypeId}, dto={JsonSerializer.Serialize(dto)}");
            await _promptTemplateDomainService.EnsureDocumentTypeExistsAsync(documentTypeId);
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();
            var inputs = dto.Fields
                .Select(field => new PromptFieldInput(field.DocumentFieldId, field.Description, field.Example, field.NormalizeRule, field.IsRequired, field.IsActive))
                .ToList();

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _promptTemplateRepository.LockDocumentTypeAsync(documentTypeId);
                var draft = await _promptTemplateDomainService.GetOrCreateDraftAsync(documentTypeId, currentUserId, now);
                draft.UpdateDraft(dto.SystemPrompt, dto.InstructionPrompt, dto.Note, currentUserId, now);
                var values = PromptTemplateDomainService.BuildFieldConfigs(await _documentFieldRepository.GetByDocumentTypeIdAsync(documentTypeId), inputs);
                await _unitOfWork.SaveChangesAsync();

                // Xoá cấu hình bị bỏ và lưu trước khi sửa / thêm: unique (PromptTemplateId, OutputKey) chỉ bỏ qua dòng đã xoá khi việc xoá đã xuống DB
                var valueByFieldId = values.ToDictionary(value => value.DocumentFieldId);
                var configs = await _promptFieldConfigRepository.GetByTemplateIdAsync(draft.Id);
                var removedConfigs = configs.Where(config => !valueByFieldId.ContainsKey(config.DocumentFieldId)).ToList();
                foreach (var removedConfig in removedConfigs)
                {
                    removedConfig.Delete(currentUserId, now);
                }
                await _unitOfWork.SaveChangesAsync();

                var configByFieldId = configs.Except(removedConfigs).ToDictionary(config => config.DocumentFieldId);
                foreach (var value in values.Where(value => configByFieldId.ContainsKey(value.DocumentFieldId)))
                {
                    configByFieldId[value.DocumentFieldId].Update(value.OutputKey, value.Description, value.Example, value.NormalizeRule, value.IsRequired,
                        value.Order, value.IsActive, currentUserId, now);
                }
                _promptFieldConfigRepository.AddRange(PromptTemplateDomainService.CreateFieldConfigs(draft,
                    values.Where(value => !configByFieldId.ContainsKey(value.DocumentFieldId)).ToList(), currentUserId, now));

                AddAuditLog(AuditLogConstants.ACTION_PROMPT_TEMPLATE_SAVE_DRAFT, AuditLogConstants.ACTION_TYPE_UPDATE,
                    $"Lưu nháp prompt OCR phiên bản {draft.Version} của metadata Id = {documentTypeId}",
                    null, new { draft.Id, draft.DocumentTypeId, draft.Version, draft.Note, Fields = values });
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
                return draft.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(SaveDraft)}, documentTypeId={documentTypeId}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Xuất bản nháp: bản đang dùng cũ → ARCHIVED, nháp → PUBLISHED (21.3, 21.9)
        public async Task Publish(int id, PublishPromptTemplateDto dto)
        {
            _logger.LogInformation($"{nameof(Publish)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            var template = await _promptTemplateRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.AiErrorPromptTemplateNotFound);
            await _promptTemplateDomainService.EnsureDocumentTypeExistsAsync(template.DocumentTypeId);
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _promptTemplateRepository.LockDocumentTypeAsync(template.DocumentTypeId);
                var draft = await _promptTemplateDomainService.EnsureDraftAsync(template);
                PromptTemplateDomainService.EnsurePublishable(await _promptFieldConfigRepository.GetByTemplateIdAsync(draft.Id),
                    await _documentFieldRepository.GetByDocumentTypeIdAsync(draft.DocumentTypeId));

                // Hạ bản cũ xuống DB trước khi nâng nháp: unique index chỉ cho một bản PUBLISHED mỗi metadata
                var published = await _promptTemplateRepository.FindPublishedAsync(draft.DocumentTypeId);
                published?.Archive(currentUserId, now);
                await _unitOfWork.SaveChangesAsync();

                draft.Publish(dto.Note, currentUserId, now);
                AddAuditLog(AuditLogConstants.ACTION_PROMPT_TEMPLATE_PUBLISH, AuditLogConstants.ACTION_TYPE_UPDATE,
                    $"Xuất bản prompt OCR phiên bản {draft.Version} của metadata Id = {draft.DocumentTypeId}",
                    new { PublishedId = published?.Id, PublishedVersion = published?.Version },
                    new { draft.Id, draft.Version, draft.Status, draft.Note });
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Publish)}, id={id}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Khôi phục bản ARCHIVED / PUBLISHED thành nháp mới, chỉ chép cấu hình trường còn hợp lệ (21.10)
        public async Task<int> Restore(int id)
        {
            _logger.LogInformation($"{nameof(Restore)}, id={id}");
            var source = await _promptTemplateRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.AiErrorPromptTemplateNotFound);
            await _promptTemplateDomainService.EnsureDocumentTypeExistsAsync(source.DocumentTypeId);
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _promptTemplateRepository.LockDocumentTypeAsync(source.DocumentTypeId);
                var draft = await _promptTemplateDomainService.CreateRestoredDraftAsync(source, currentUserId, now);
                await _unitOfWork.SaveChangesAsync();

                var values = PromptTemplateDomainService.BuildRestoredFieldConfigs(await _promptFieldConfigRepository.GetByTemplateIdAsync(source.Id),
                    await _documentFieldRepository.GetByDocumentTypeIdAsync(source.DocumentTypeId));
                _promptFieldConfigRepository.AddRange(PromptTemplateDomainService.CreateFieldConfigs(draft, values, currentUserId, now));
                AddAuditLog(AuditLogConstants.ACTION_PROMPT_TEMPLATE_RESTORE, AuditLogConstants.ACTION_TYPE_CREATE,
                    $"Khôi phục prompt OCR phiên bản {source.Version} thành nháp phiên bản {draft.Version} của metadata Id = {source.DocumentTypeId}",
                    null, new { draft.Id, draft.Version, SourceTemplateId = source.Id, SourceVersion = source.Version, Fields = values });
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
                return draft.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Restore)}, id={id}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Bỏ nháp: xoá mềm bản nháp và cấu hình trường của nó (21.12)
        public async Task Discard(int id)
        {
            _logger.LogInformation($"{nameof(Discard)}, id={id}");
            var template = await _promptTemplateRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.AiErrorPromptTemplateNotFound);
            template.EnsureDraft();
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _promptTemplateRepository.LockDocumentTypeAsync(template.DocumentTypeId);
                var draft = await _promptTemplateDomainService.EnsureDraftAsync(template);
                draft.Delete(currentUserId, now);
                foreach (var config in await _promptFieldConfigRepository.GetByTemplateIdAsync(draft.Id))
                {
                    config.Delete(currentUserId, now);
                }
                AddAuditLog(AuditLogConstants.ACTION_PROMPT_TEMPLATE_DISCARD, AuditLogConstants.ACTION_TYPE_DELETE,
                    $"Bỏ nháp prompt OCR phiên bản {draft.Version} của metadata Id = {draft.DocumentTypeId}",
                    new { draft.Id, draft.Version, draft.Note }, null);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Discard)}, id={id}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Xem trước prompt ghép từ một bản, bỏ cấu hình trường không hợp lệ và trường tạm tắt (21.11)
        public async Task<ViewComposedPromptDto> Preview(int id)
        {
            _logger.LogInformation($"{nameof(Preview)}, id={id}");
            var template = await _promptTemplateRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.AiErrorPromptTemplateNotFound);
            var composedPrompt = PromptTemplateDomainService.Compose(template, await _promptFieldConfigRepository.GetByTemplateIdAsync(template.Id),
                await _documentFieldRepository.GetByDocumentTypeIdAsync(template.DocumentTypeId));
            return new ViewComposedPromptDto
            {
                SystemPrompt = composedPrompt.SystemPrompt,
                Prompt = composedPrompt.Prompt,
            };
        }
    }
}
