using sh.be.shared.Constants.Construction;

namespace sh.be.domain.Module.Construction.Submissions.Services.Import.Models
{
    public record ExistingSubmissionSnapshot(int Id, string FileName, string ProfileRange, string Box, int StatusCheck)
    {
        public string BuildKey()
        {
            return SubmissionKeyPolicy.Build(FileName, ProfileRange, Box);
        }

        public bool IsChecked()
        {
            return StatusCheck == SubmissionConst.CHECKED;
        }
    }
}
