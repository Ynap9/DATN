namespace sh.be.domain.Module.Construction.Services.Import.Models
{
    public record AppendImportPlan(IReadOnlyList<NewSubmissionDraft> NewSubmissions, int MappedFieldCount, int NewDataCount,
        int NonEmptyCellCount, int ExistingSubmissionCount, int DistinctSheetKeyCount)
    {
        public bool HasChanges => NewSubmissions.Count > 0;
    }
}
