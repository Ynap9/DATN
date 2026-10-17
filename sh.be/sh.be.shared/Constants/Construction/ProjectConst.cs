namespace sh.be.shared.Constants.Construction
{
    public static class ProjectConst
    {
        public const int DEPLOYING = 1;
        public const int DOING = 2;
        public const int ACCEPTING = 3;
        public const int DONE = 4;

        public static readonly IReadOnlyList<int> STATUSES = [DEPLOYING, DOING, ACCEPTING, DONE];
    }
}
