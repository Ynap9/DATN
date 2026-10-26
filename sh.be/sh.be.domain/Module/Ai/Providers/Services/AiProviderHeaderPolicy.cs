using sh.be.shared.Constants.Ai;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace sh.be.domain.Module.Ai.Providers.Services
{
    public static class AiProviderHeaderPolicy
    {
        public static string? Normalize(Dictionary<string, string>? headers, string? apiKeyHeaderName)
        {
            if (headers == null || headers.Count == 0)
            {
                return null;
            }
            if (headers.Count > AiProviderConst.EXTRA_HEADERS_MAX_ITEMS)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Header thêm tối đa {AiProviderConst.EXTRA_HEADERS_MAX_ITEMS} cặp");
            }

            var normalizedApiKeyHeaderName = apiKeyHeaderName?.Trim();
            var normalizedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, value) in headers)
            {
                var headerName = name.Trim();
                if (!Regex.IsMatch(headerName, AiProviderConst.API_KEY_HEADER_NAME_PATTERN))
                {
                    throw new UserFriendlyException(ErrorCodes.BadRequest, $"Tên header \"{headerName}\" chỉ gồm chữ, số, dấu gạch ngang, tối đa 100 ký tự");
                }
                if (AiProviderConst.FORBIDDEN_HEADERS.Contains(headerName, StringComparer.OrdinalIgnoreCase)
                    || String.Equals(headerName, normalizedApiKeyHeaderName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UserFriendlyException(ErrorCodes.BadRequest, $"Header {headerName} không được khai ở header thêm, hãy dùng API key");
                }
                var headerValue = value?.Trim() ?? String.Empty;
                if (headerValue.Length > AiProviderConst.EXTRA_HEADER_VALUE_MAX_LENGTH || headerValue.Contains('\r') || headerValue.Contains('\n'))
                {
                    throw new UserFriendlyException(ErrorCodes.BadRequest,
                        $"Giá trị header {headerName} tối đa {AiProviderConst.EXTRA_HEADER_VALUE_MAX_LENGTH} ký tự, không xuống dòng");
                }
                if (!normalizedHeaders.TryAdd(headerName, headerValue))
                {
                    throw new UserFriendlyException(ErrorCodes.BadRequest, $"Header {headerName} bị khai trùng");
                }
            }

            var headersJson = JsonSerializer.Serialize(normalizedHeaders);
            if (headersJson.Length > AiProviderConst.EXTRA_HEADERS_JSON_MAX_LENGTH)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Header thêm quá dài, tối đa {AiProviderConst.EXTRA_HEADERS_JSON_MAX_LENGTH} ký tự");
            }
            return headersJson;
        }
    }
}
