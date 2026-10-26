using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Ai;
using sh.be.shared.Constants.Db;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace sh.be.domain.Module.Ai.Providers.Entities
{
    [Table(nameof(AiProvider), Schema = DbSchemas.Ai)]
    [Index(nameof(Code), IsUnique = true, Name = UniqueCodeIndexName)]
    public class AiProvider : ISoftDeleted
    {
        public const string UniqueCodeIndexName = $"IX_{nameof(AiProvider)}_{nameof(Code)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        [MaxLength(AiProviderConst.CODE_MAX_LENGTH)]
        public string Code { get; private set; } = String.Empty;
        [MaxLength(AiProviderConst.NAME_MAX_LENGTH)]
        public string Name { get; private set; } = String.Empty;
        [MaxLength(AiProviderConst.PROTOCOL_MAX_LENGTH)]
        public string Protocol { get; private set; } = String.Empty;
        [MaxLength(AiProviderConst.BASE_URL_MAX_LENGTH)]
        public string BaseUrl { get; private set; } = String.Empty;
        [MaxLength(AiProviderConst.SCOPE_MAX_LENGTH)]
        public string Scope { get; private set; } = String.Empty;
        public string? ApiKeyEncrypted { get; private set; }
        [MaxLength(AiProviderConst.API_KEY_HASH_LENGTH)]
        public string? ApiKeyHash { get; private set; }
        public bool HasApiKey { get; private set; }
        [MaxLength(AiProviderConst.API_KEY_HEADER_NAME_MAX_LENGTH)]
        public string? ApiKeyHeaderName { get; private set; }
        [MaxLength(AiProviderConst.API_KEY_HEADER_PREFIX_MAX_LENGTH)]
        public string? ApiKeyHeaderPrefix { get; private set; }
        public int TimeoutSeconds { get; private set; }
        public int MaxConcurrency { get; private set; }
        public int? RateLimitPerMinute { get; private set; }
        [MaxLength(AiProviderConst.EXTRA_HEADERS_JSON_MAX_LENGTH)]
        public string? ExtraHeadersJson { get; private set; }
        public bool IsActive { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private AiProvider()
        {
        }

        public static AiProvider Create(string code, string name, string protocol, string baseUrl, string scope, string? apiKeyHeaderName,
            string? apiKeyHeaderPrefix, int? timeoutSeconds, int? maxConcurrency, int? rateLimitPerMinute, string? extraHeadersJson, bool isActive,
            string? createdBy, DateTime createdDate)
        {
            var normalizedCode = code.Trim().ToLowerInvariant();
            if (!Regex.IsMatch(normalizedCode, AiProviderConst.CODE_PATTERN))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Mã chỉ gồm chữ thường, số, dấu gạch ngang, dài 2–50 ký tự");
            }
            var provider = new AiProvider
            {
                Code = normalizedCode,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
            provider.SetValues(name, protocol, baseUrl, scope, apiKeyHeaderName, apiKeyHeaderPrefix, timeoutSeconds ?? AiProviderConst.TIMEOUT_DEFAULT,
                maxConcurrency ?? AiProviderConst.MAX_CONCURRENCY_DEFAULT, rateLimitPerMinute, extraHeadersJson, isActive);
            return provider;
        }

        public void Update(string name, string protocol, string baseUrl, string scope, string? apiKeyHeaderName, string? apiKeyHeaderPrefix,
            int timeoutSeconds, int maxConcurrency, int? rateLimitPerMinute, string? extraHeadersJson, bool isActive, string? modifiedBy, DateTime modifiedDate)
        {
            SetValues(name, protocol, baseUrl, scope, apiKeyHeaderName, apiKeyHeaderPrefix, timeoutSeconds, maxConcurrency, rateLimitPerMinute,
                extraHeadersJson, isActive);
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void SetApiKey(string apiKeyEncrypted, string apiKeyHash, string? modifiedBy, DateTime modifiedDate)
        {
            ApiKeyEncrypted = apiKeyEncrypted;
            ApiKeyHash = apiKeyHash;
            HasApiKey = true;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void ClearApiKey(string? modifiedBy, DateTime modifiedDate)
        {
            ApiKeyEncrypted = null;
            ApiKeyHash = null;
            HasApiKey = false;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public string GetApiKeyEncrypted()
        {
            if (!HasApiKey || ApiKeyEncrypted == null)
            {
                throw new UserFriendlyException(ErrorCodes.AiErrorProviderApiKeyMissing);
            }
            return ApiKeyEncrypted;
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            ClearApiKey(deletedBy, deletedDate);
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }

        private void SetValues(string name, string protocol, string baseUrl, string scope, string? apiKeyHeaderName, string? apiKeyHeaderPrefix,
            int timeoutSeconds, int maxConcurrency, int? rateLimitPerMinute, string? extraHeadersJson, bool isActive)
        {
            var trimmedName = name.Trim();
            if (trimmedName.Length == 0 || trimmedName.Length > AiProviderConst.NAME_MAX_LENGTH)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Tên bắt buộc, tối đa {AiProviderConst.NAME_MAX_LENGTH} ký tự");
            }
            if (!AiProviderConst.PROTOCOLS.Contains(protocol))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Giao thức không hợp lệ");
            }
            if (!AiProviderConst.SCOPES.Contains(scope))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Phạm vi phải là LOCAL hoặc PUBLIC");
            }

            var trimmedBaseUrl = baseUrl.Trim();
            if (!Uri.TryCreate(trimmedBaseUrl, UriKind.Absolute, out var baseUri)
                || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps)
                || trimmedBaseUrl.Contains('?') || trimmedBaseUrl.Contains('#'))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Địa chỉ gọi phải là URL http/https tuyệt đối, không có query string hoặc fragment");
            }
            if (!String.IsNullOrEmpty(baseUri.UserInfo))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Địa chỉ gọi không được chứa tài khoản, mật khẩu; hãy dùng API key");
            }
            var normalizedBaseUrl = trimmedBaseUrl.TrimEnd('/');
            if (normalizedBaseUrl.Length > AiProviderConst.BASE_URL_MAX_LENGTH)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Địa chỉ gọi tối đa {AiProviderConst.BASE_URL_MAX_LENGTH} ký tự");
            }

            if (timeoutSeconds < AiProviderConst.TIMEOUT_MIN || timeoutSeconds > AiProviderConst.TIMEOUT_MAX)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Thời gian chờ phải từ {AiProviderConst.TIMEOUT_MIN} đến {AiProviderConst.TIMEOUT_MAX} giây");
            }
            if (maxConcurrency < AiProviderConst.MAX_CONCURRENCY_MIN || maxConcurrency > AiProviderConst.MAX_CONCURRENCY_MAX)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Số lượt gọi đồng thời phải từ {AiProviderConst.MAX_CONCURRENCY_MIN} đến {AiProviderConst.MAX_CONCURRENCY_MAX}");
            }
            if (rateLimitPerMinute.HasValue && (rateLimitPerMinute < AiProviderConst.RATE_LIMIT_MIN || rateLimitPerMinute > AiProviderConst.RATE_LIMIT_MAX))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Giới hạn lượt gọi mỗi phút phải từ {AiProviderConst.RATE_LIMIT_MIN} đến {AiProviderConst.RATE_LIMIT_MAX} hoặc để trống");
            }

            var normalizedHeaderName = String.IsNullOrWhiteSpace(apiKeyHeaderName) ? null : apiKeyHeaderName.Trim();
            var normalizedHeaderPrefix = String.IsNullOrEmpty(apiKeyHeaderPrefix) ? null : apiKeyHeaderPrefix;
            if (protocol != AiProviderConst.PROTOCOL_CUSTOM_HTTP && (normalizedHeaderName != null || normalizedHeaderPrefix != null))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Giao thức {protocol} tự gắn API key, không khai tên header và tiền tố");
            }
            if (normalizedHeaderName != null && !Regex.IsMatch(normalizedHeaderName, AiProviderConst.API_KEY_HEADER_NAME_PATTERN))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Tên header API key chỉ gồm chữ, số, dấu gạch ngang, tối đa 100 ký tự");
            }
            if (normalizedHeaderPrefix != null && normalizedHeaderName == null)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Tiền tố API key chỉ khai khi có tên header API key");
            }
            if (normalizedHeaderPrefix != null && normalizedHeaderPrefix.Length > AiProviderConst.API_KEY_HEADER_PREFIX_MAX_LENGTH)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Tiền tố API key tối đa {AiProviderConst.API_KEY_HEADER_PREFIX_MAX_LENGTH} ký tự");
            }

            Name = trimmedName;
            Protocol = protocol;
            BaseUrl = normalizedBaseUrl;
            Scope = scope;
            ApiKeyHeaderName = normalizedHeaderName;
            ApiKeyHeaderPrefix = normalizedHeaderPrefix;
            TimeoutSeconds = timeoutSeconds;
            MaxConcurrency = maxConcurrency;
            RateLimitPerMinute = rateLimitPerMinute;
            ExtraHeadersJson = extraHeadersJson;
            IsActive = isActive;
        }
    }
}
