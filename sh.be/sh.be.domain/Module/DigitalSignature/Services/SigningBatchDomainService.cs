using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.DigitalSignature.Services
{
    public static class SigningBatchDomainService
    {
        public static void EnsureUploadCount(int fileCount)
        {
            if (fileCount > SigningBatchConst.MAX_FILES_PER_UPLOAD)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorTooManyUploadFiles);
            }
        }

        public static bool IsAcceptedFile(string fileName, long length)
        {
            return length > 0 && fileName.EndsWith(SigningBatchConst.PDF_EXTENSION, StringComparison.OrdinalIgnoreCase);
        }

        public static string BuildSourceObjectKey(int signingBatchId, int order)
        {
            return $"{SigningBatchConst.OBJECT_KEY_PREFIX}/{signingBatchId}/{SigningBatchConst.SOURCE_FOLDER}/{order:D6}{SigningBatchConst.PDF_EXTENSION}";
        }

        public static void EnsureSignable(bool hasSignature, bool allowSigningSignedPdf)
        {
            if (hasSignature && !allowSigningSignedPdf)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfAlreadySigned);
            }
        }

        public static string BuildSignedObjectKey(int signingBatchId, int order)
        {
            return $"{SigningBatchConst.OBJECT_KEY_PREFIX}/{signingBatchId}/{SigningBatchConst.SIGNED_FOLDER}/{order:D6}{SigningBatchConst.PDF_EXTENSION}";
        }

        public static string ToErrorMessage(Exception exception)
        {
            var message = exception is UserFriendlyException userFriendlyException
                ? ErrorMessages.GetMessage(userFriendlyException.ErrorCode)
                : exception.Message;
            return message.Length > SigningBatchConst.MAX_ERROR_MESSAGE_LENGTH ? message[..SigningBatchConst.MAX_ERROR_MESSAGE_LENGTH] : message;
        }

        public static IReadOnlyList<string> ResolveSupportedPluginVersions(IReadOnlyList<string> configuredVersions)
        {
            return configuredVersions.Count > 0 ? configuredVersions : PluginConst.DEFAULT_SUPPORTED_VERSIONS;
        }

        public static string? ResolveUnsupportedPluginReason(string version, IReadOnlyList<string> supportedVersions)
        {
            if (version.Length == 0)
            {
                return PluginConst.VERSION_MISSING_REASON;
            }
            return supportedVersions.Any(v => string.Equals(v.Trim(), version, StringComparison.OrdinalIgnoreCase))
                ? null
                : string.Format(PluginConst.VERSION_UNSUPPORTED_REASON, version);
        }
    }
}
