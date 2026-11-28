namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewVerifyImportOverwriteSubmissionDto
    {
        public int TotalOverWriteCount { get; set; }
        public int TotalSkippedChecked { get; set; }
        public List<OutOfEnumFieldDto> OutOfEnum { get; set; } = new();
    }
}
