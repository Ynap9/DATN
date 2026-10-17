namespace sh.be.shared.Constants.DigitalSignature
{
    public static class SigningBatchFileConst
    {
        public const int PENDING = 0;
        public const int SIGNING = 1;
        public const int DONE = 2;
        public const int FAILED = 3;

        public static readonly IReadOnlyList<int> STATUSES = [PENDING, SIGNING, DONE, FAILED];
    }
}
