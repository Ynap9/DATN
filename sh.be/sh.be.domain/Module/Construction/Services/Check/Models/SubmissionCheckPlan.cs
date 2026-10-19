using sh.be.domain.Module.Construction.Entities;

namespace sh.be.domain.Module.Construction.Services.Check.Models
{
    public record SubmissionCheckPlan(IReadOnlyList<SubmissionData> DatasToUpdate, IReadOnlyList<SubmissionData> DatasToInsert,
        IReadOnlyList<SubmissionDataChange> Changes)
    {
        public int ChangedDataCount => DatasToUpdate.Count + DatasToInsert.Count;
    }
}
