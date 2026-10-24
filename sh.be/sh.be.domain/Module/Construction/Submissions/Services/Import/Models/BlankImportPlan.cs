using sh.be.domain.Module.Construction.Submissions.Entities;

namespace sh.be.domain.Module.Construction.Submissions.Services.Import.Models
{
    public record BlankImportPlan(IReadOnlyList<SubmissionData> DatasToUpdate, IReadOnlyList<SubmissionData> DatasToInsert, int MappedFieldCount)
    {
        public int ChangedDataCount => DatasToUpdate.Count + DatasToInsert.Count;

        public bool HasChanges => ChangedDataCount > 0;
    }
}
