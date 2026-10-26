using sh.be.shared.Constants.Ai;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Security.Cryptography;
using System.Text;

namespace sh.be.domain.Module.Ai.Providers.Services
{
    public static class AiProviderApiKeyPolicy
    {
        public static string Validate(string apiKey)
        {
            var trimmedApiKey = apiKey.Trim();
            if (trimmedApiKey.Length == 0 || trimmedApiKey.Length > AiProviderConst.API_KEY_MAX_LENGTH || trimmedApiKey.Any(Char.IsWhiteSpace))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"API key dài 1–{AiProviderConst.API_KEY_MAX_LENGTH} ký tự, không chứa khoảng trắng hoặc xuống dòng");
            }
            return trimmedApiKey;
        }

        public static string Hash(string apiKey)
        {
            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));
        }

        public static string? Fingerprint(string? apiKeyHash)
        {
            return apiKeyHash?[..AiProviderConst.API_KEY_FINGERPRINT_LENGTH];
        }
    }
}
