namespace sh.be.domain.Module.Construction.Submissions.Services.Import.Models
{
    public record OverwriteChange(int SubmissionId, string FileName, int DocumentFieldId, string OldData, string NewData);
}
