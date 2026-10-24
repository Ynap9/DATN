using sh.be.shared.Constants.Packaging;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Packaging.Services
{
    public static class PackageSchemaPolicy
    {
        public static void EnsureValidPackageType(int packageType)
        {
            if (!PackageConst.PACKAGE_TYPES.Contains(packageType))
            {
                throw new UserFriendlyException(ErrorCodes.PackagingErrorPackageTypeInvalid);
            }
        }

        public static void EnsureValidMetadataTypes(IEnumerable<int> metadataTypes)
        {
            if (metadataTypes.Any(metadataType => !PackageSchemaConst.METADATA_TYPES.Contains(metadataType)))
            {
                throw new UserFriendlyException(ErrorCodes.PackagingErrorMetadataTypeInvalid);
            }
        }
    }
}
