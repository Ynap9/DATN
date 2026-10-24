using sh.be.domain.Base.Models;

namespace sh.be.domain.Module.Construction.Submissions.Services.Import.Models
{
    public record SubmissionImportColumns(int FileNameColumnIndex, int? ProfileRangeColumnIndex, int? BoxColumnIndex, int? FileUrlColumnIndex)
    {
        public string GetFileName(SheetRow row)
        {
            return row.GetCell(FileNameColumnIndex);
        }

        public string GetProfileRange(SheetRow row)
        {
            return GetOptionalCell(row, ProfileRangeColumnIndex);
        }

        public string GetBox(SheetRow row)
        {
            return GetOptionalCell(row, BoxColumnIndex);
        }

        public string GetFileUrl(SheetRow row)
        {
            return GetOptionalCell(row, FileUrlColumnIndex);
        }

        public string BuildKey(SheetRow row)
        {
            return SubmissionKeyPolicy.Build(GetFileName(row), GetProfileRange(row), GetBox(row));
        }

        private static string GetOptionalCell(SheetRow row, int? columnIndex)
        {
            return columnIndex.HasValue ? row.GetCell(columnIndex.Value) : String.Empty;
        }
    }
}
