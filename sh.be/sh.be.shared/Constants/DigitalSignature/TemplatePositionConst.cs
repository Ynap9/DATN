namespace sh.be.shared.Constants.DigitalSignature
{
    public static class TemplatePositionConst
    {
        public const int DIGITAL_SIGNATURE = 0;
        public const int STAMP = 1;
        public const int WET_SIGNATURE = 2;

        public static readonly IReadOnlyList<int> KINDS = [DIGITAL_SIGNATURE, STAMP, WET_SIGNATURE];
    }
}
