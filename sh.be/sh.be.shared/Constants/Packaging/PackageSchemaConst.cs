namespace sh.be.shared.Constants.Packaging
{
    public static class PackageSchemaConst
    {
        public const int METADATA_PACKAGE = 1;
        public const int METADATA_TEXT = 2;
        public const int METADATA_PICTURE = 3;
        public const int METADATA_MEDIA = 4;

        public static readonly IReadOnlyList<int> METADATA_TYPES = [METADATA_PACKAGE, METADATA_TEXT, METADATA_PICTURE, METADATA_MEDIA];

        public const int NAME_MAX_LENGTH = 31;
    }
}
