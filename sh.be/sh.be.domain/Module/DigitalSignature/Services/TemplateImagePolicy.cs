using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.DigitalSignature.Services
{
    public static class TemplateImagePolicy
    {
        public static void EnsureValidImage(string fileName, long length)
        {
            if (length <= 0
                || length > TemplateConst.MAX_IMAGE_BYTES
                || !TemplateConst.IMAGE_EXTENSIONS.Contains(GetExtension(fileName)))
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorInvalidTemplateImage);
            }
        }

        public static string BuildObjectKey(int templateId, string imageName, string fileName)
        {
            return $"{BuildObjectKeyPrefix(templateId)}{imageName}{GetExtension(fileName)}";
        }

        public static string BuildObjectKeyPrefix(int templateId)
        {
            return $"{TemplateConst.IMAGE_OBJECT_KEY_PREFIX}/{templateId}/";
        }

        private static string GetExtension(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant();
        }
    }
}
