using sh.be.shared.Constants.Construction;

namespace sh.be.domain.Module.Construction.Services.Import
{
    public static class SubmissionKeyPolicy
    {
        public static string Build(string fileName, string profileRange, string box)
        {
            return string.Join(SubmissionConst.KEY_SEPARATOR,
                fileName.Trim().ToLowerInvariant(),
                profileRange.Trim().ToLowerInvariant(),
                box.Trim().ToLowerInvariant());
        }
    }
}
