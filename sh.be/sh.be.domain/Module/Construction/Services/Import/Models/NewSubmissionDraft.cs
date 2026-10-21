using sh.be.domain.Module.Construction.Entities;

namespace sh.be.domain.Module.Construction.Services.Import.Models
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
