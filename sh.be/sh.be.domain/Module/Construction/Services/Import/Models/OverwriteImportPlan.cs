using sh.be.domain.Module.Construction.Entities;

namespace sh.be.domain.Module.Construction.Services.Import.Models
{
    public record OverwriteImportPlan(IReadOnlyList<SubmissionData> DatasToUpdate, IReadOnlyList<SubmissionData> DatasToInsert,
        IReadOnlyList<OverwriteChange> Changes, int SkippedCheckedCount)
    {
        public int ChangedDataCount => DatasToUpdate.Count + DatasToInsert.Count;

        public bool HasChanges => ChangedDataCount > 0;
    }
}
