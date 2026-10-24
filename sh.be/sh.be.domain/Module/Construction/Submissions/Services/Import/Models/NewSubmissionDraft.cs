using sh.be.domain.Module.Construction.Submissions.Entities;

namespace sh.be.domain.Module.Construction.Submissions.Services.Import.Models
{
    public record NewSubmissionDraft(Submission Submission, IReadOnlyList<SubmissionData> Datas)
    {
        public IReadOnlyList<SubmissionData> AssignDatasToSubmission()
        {
            foreach (var data in Datas)
            {
                data.AssignSubmission(Submission.Id);
            }
            return Datas;
        }
    }
}
