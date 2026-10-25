namespace sh.be.shared.Constants.Packaging
{
    public static class PackageConst
    {
        public const int AIP_HOSO = 1;
        public const int AIP_TAILIEU = 2;
        public const int SIP_HOSO = 3;
        public const int SIP_TAILIEU = 4;
        public const int DIP = 5;

        public static readonly IReadOnlyList<int> PACKAGE_TYPES = [AIP_HOSO, AIP_TAILIEU, SIP_HOSO, SIP_TAILIEU, DIP];

        public static readonly IReadOnlyDictionary<int, string> PACKAGE_TYPE_CODES = new Dictionary<int, string>
        {
            [AIP_HOSO] = "AIP_hoso",
            [AIP_TAILIEU] = "AIP_tailieu",
            [SIP_HOSO] = "SIP_hoso",
            [SIP_TAILIEU] = "SIP_tailieu",
            [DIP] = "DIP",
        };

        public static readonly IReadOnlyDictionary<int, string> PACKAGE_TYPE_NAMES = new Dictionary<int, string>
        {
            [AIP_HOSO] = "Gói hồ sơ lưu trữ (AIP_hoso)",
            [AIP_TAILIEU] = "Gói tài liệu lưu trữ (AIP_tailieu)",
            [SIP_HOSO] = "Gói hồ sơ nộp (SIP_hoso)",
            [SIP_TAILIEU] = "Gói tài liệu nộp (SIP_tailieu)",
            [DIP] = "Gói tài liệu lưu trữ sử dụng (DIP)",
        };
    }
}
