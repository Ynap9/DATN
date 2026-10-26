using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.Ai;
using sh.be.applications.Module.Ai.Dtos.AiProvider;
using sh.be.applications.Module.Ai.Interfaces.Queries;
using sh.be.applications.Module.Ai.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Ai.Providers.Entities;
using sh.be.domain.Module.Ai.Providers.Repositories;
using sh.be.domain.Module.Ai.Providers.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Ai;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace sh.be.applications.Module.Ai.Implements
{
    public class AiProviderService : BaseService, IAiProviderService
    {
        private const string JsonContentType = "application/json";
        private static readonly JsonSerializerOptions ApiKeyFileJsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private readonly IAiProviderRepository _aiProviderRepository;
        private readonly IAiProviderQuery _aiProviderQuery;
        private readonly AiProviderDomainService _aiProviderDomainService;
        private readonly ISecretProtectorExternal _secretProtectorExternal;

        public AiProviderService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<AiProviderService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IAiProviderRepository aiProviderRepository, IAiProviderQuery aiProviderQuery, AiProviderDomainService aiProviderDomainService, ISecretProtectorExternal secretProtectorExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _aiProviderRepository = aiProviderRepository;
            _aiProviderQuery = aiProviderQuery;
            _aiProviderDomainService = aiProviderDomainService;
            _secretProtectorExternal = secretProtectorExternal;
        }

        public async Task<BaseResponsePagingDto<GetListAiProviderDto>> FindPaging(FindPagingAiProviderDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > AiProviderConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {AiProviderConst.MAX_PAGE_SIZE}");
            }
            if (dto.Protocol != null && !AiProviderConst.PROTOCOLS.Contains(dto.Protocol))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Giao thức không hợp lệ");
            }
            if (dto.Scope != null && !AiProviderConst.SCOPES.Contains(dto.Scope))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Phạm vi phải là LOCAL hoặc PUBLIC");
            }
            return await _aiProviderQuery.FindPaging(dto);
        }

        public async Task<List<GetListAiProviderDropDownDto>> GetDropDown()
        {
            _logger.LogInformation($"{nameof(GetDropDown)}");
            return await _aiProviderQuery.GetDropDown();
        }

        public async Task<FindAiProviderByIdDto> FindById(int id)
        {
            _logger.LogInformation($"{nameof(FindById)}, id={id}");
            return await _aiProviderQuery.FindById(id)
                ?? throw new UserFriendlyException(ErrorCodes.AiErrorProviderNotFound);
        }

        public async Task<int> Create(CreateAiProviderDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, dto={JsonSerializer.Serialize(dto)}");
            var extraHeadersJson = AiProviderHeaderPolicy.Normalize(dto.ExtraHeaders, dto.ApiKeyHeaderName);
            var aiProvider = AiProvider.Create(dto.Code, dto.Name, dto.Protocol, dto.BaseUrl, dto.Scope, dto.ApiKeyHeaderName, dto.ApiKeyHeaderPrefix,
                dto.TimeoutSeconds, dto.MaxConcurrency, dto.RateLimitPerMinute, extraHeadersJson, dto.IsActive, getCurrentUserId(), GetVietnamTime());
            await _aiProviderDomainService.EnsureUniqueCodeAsync(aiProvider.Code);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                _aiProviderRepository.Add(aiProvider);
                await _unitOfWork.SaveChangesAsync();

                AddAuditLog(AuditLogConstants.ACTION_AI_PROVIDER_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Tạo nhà cung cấp AI {aiProvider.Code}",
                    null, CreateAuditSnapshot(aiProvider));
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Create)}, code={aiProvider.Code}");
                await transaction.RollbackAsync();
                throw;
            }
            return aiProvider.Id;
        }

        public async Task Update(int id, UpdateAiProviderDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            var aiProvider = await _aiProviderRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.AiErrorProviderNotFound);
            var oldData = CreateAuditSnapshot(aiProvider);
            var oldScope = aiProvider.Scope;

            var extraHeadersJson = AiProviderHeaderPolicy.Normalize(dto.ExtraHeaders, dto.ApiKeyHeaderName);
            await _aiProviderDomainService.EnsureProtocolChangeAllowedAsync(aiProvider, dto.Protocol);
            await _aiProviderDomainService.EnsureCanDeactivateAsync(aiProvider, dto.IsActive);
            aiProvider.Update(dto.Name, dto.Protocol, dto.BaseUrl, dto.Scope, dto.ApiKeyHeaderName, dto.ApiKeyHeaderPrefix, dto.TimeoutSeconds,
                dto.MaxConcurrency, dto.RateLimitPerMinute, extraHeadersJson, dto.IsActive, getCurrentUserId(), GetVietnamTime());

            var scopeChangeContent = oldScope == aiProvider.Scope ? String.Empty : $". Đổi phạm vi {oldScope} -> {aiProvider.Scope}";
            _aiProviderRepository.Update(aiProvider);
            AddAuditLog(AuditLogConstants.ACTION_AI_PROVIDER_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật nhà cung cấp AI {aiProvider.Code}{scopeChangeContent}",
                oldData, CreateAuditSnapshot(aiProvider));
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<ViewAiProviderApiKeyResultDto> UpdateApiKey(int id, UpdateAiProviderApiKeyDto dto)
        {
            var hasApiKey = !String.IsNullOrWhiteSpace(dto.ApiKey);
            _logger.LogInformation($"{nameof(UpdateApiKey)}, id={id}, hasApiKey={hasApiKey}");
            var aiProvider = await _aiProviderRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.AiErrorProviderNotFound);
            var oldData = new { aiProvider.HasApiKey, ApiKeyFingerprint = AiProviderApiKeyPolicy.Fingerprint(aiProvider.ApiKeyHash) };

            if (hasApiKey)
            {
                var apiKey = AiProviderApiKeyPolicy.Validate(dto.ApiKey!);
                aiProvider.SetApiKey(_secretProtectorExternal.Protect(apiKey, AiProviderConst.API_KEY_PROTECTOR_PURPOSE), AiProviderApiKeyPolicy.Hash(apiKey),
                    getCurrentUserId(), GetVietnamTime());
            }
            else
            {
                aiProvider.ClearApiKey(getCurrentUserId(), GetVietnamTime());
            }

            var result = new ViewAiProviderApiKeyResultDto
            {
                HasApiKey = aiProvider.HasApiKey,
                ApiKeyFingerprint = AiProviderApiKeyPolicy.Fingerprint(aiProvider.ApiKeyHash),
            };
            _aiProviderRepository.Update(aiProvider);
            AddAuditLog(AuditLogConstants.ACTION_AI_PROVIDER_API_KEY, AuditLogConstants.ACTION_TYPE_UPDATE,
                $"{(hasApiKey ? "Đặt" : "Xóa")} API key nhà cung cấp AI {aiProvider.Code}", oldData, result);
            await _unitOfWork.SaveChangesAsync();
            return result;
        }

        public async Task Delete(int id)
        {
            _logger.LogInformation($"{nameof(Delete)}, id={id}");
            var aiProvider = await _aiProviderRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.AiErrorProviderNotFound);
            await _aiProviderDomainService.EnsureDeletableAsync(aiProvider);
            var oldData = CreateAuditSnapshot(aiProvider);

            aiProvider.Delete(getCurrentUserId(), GetVietnamTime());

            _aiProviderRepository.Update(aiProvider);
            AddAuditLog(AuditLogConstants.ACTION_AI_PROVIDER_DELETE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa nhà cung cấp AI {aiProvider.Code}",
                oldData, null);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<ExportFileDto> DownloadApiKey(int id)
        {
            _logger.LogInformation($"{nameof(DownloadApiKey)}, id={id}");
            var aiProvider = await _aiProviderRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.AiErrorProviderNotFound);
            var apiKey = _secretProtectorExternal.TryUnprotect(aiProvider.GetApiKeyEncrypted(), AiProviderConst.API_KEY_PROTECTOR_PURPOSE)
                ?? throw new UserFriendlyException(ErrorCodes.AiErrorProviderApiKeyUnreadable);

            var now = GetVietnamTime();
            var apiKeyFingerprint = AiProviderApiKeyPolicy.Fingerprint(aiProvider.ApiKeyHash);
            var apiKeyFile = new AiProviderApiKeyFileDto
            {
                Type = AiProviderConst.API_KEY_FILE_TYPE,
                ProviderCode = aiProvider.Code,
                ProviderName = aiProvider.Name,
                Protocol = aiProvider.Protocol,
                BaseUrl = aiProvider.BaseUrl,
                ApiKeyHeaderName = aiProvider.ApiKeyHeaderName,
                ApiKeyHeaderPrefix = aiProvider.ApiKeyHeaderPrefix,
                ApiKey = apiKey,
                ApiKeyFingerprint = apiKeyFingerprint,
                ExportedBy = getCurrentUserId(),
                ExportedAt = new DateTimeOffset(now, VietnamTimeZone.GetUtcOffset(now)),
            };

            AddAuditLog(AuditLogConstants.ACTION_AI_PROVIDER_API_KEY_DOWNLOAD, AuditLogConstants.ACTION_TYPE_EXPORT, $"Tải API key nhà cung cấp AI {aiProvider.Code}",
                null, new { aiProvider.Id, aiProvider.Code, ApiKeyFingerprint = apiKeyFingerprint });
            await _unitOfWork.SaveChangesAsync();

            return new ExportFileDto
            {
                FileName = String.Format(AiProviderConst.API_KEY_FILE_NAME_FORMAT, aiProvider.Code, now),
                ContentType = JsonContentType,
                Content = JsonSerializer.SerializeToUtf8Bytes(apiKeyFile, ApiKeyFileJsonOptions),
            };
        }

        private static object CreateAuditSnapshot(AiProvider aiProvider)
        {
            return new
            {
                aiProvider.Id,
                aiProvider.Code,
                aiProvider.Name,
                aiProvider.Protocol,
                aiProvider.BaseUrl,
                aiProvider.Scope,
                aiProvider.ApiKeyHeaderName,
                aiProvider.ApiKeyHeaderPrefix,
                aiProvider.TimeoutSeconds,
                aiProvider.MaxConcurrency,
                aiProvider.RateLimitPerMinute,
                aiProvider.ExtraHeadersJson,
                aiProvider.HasApiKey,
                aiProvider.IsActive,
            };
        }
    }
}
