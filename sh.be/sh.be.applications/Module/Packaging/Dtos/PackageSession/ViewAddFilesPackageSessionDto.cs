namespace sh.be.applications.Module.Packaging.Dtos.PackageSession
{
    public class ViewAddFilesPackageSessionDto
    {
        public int AcceptedCount { get; set; }
        public List<ViewPackageUploadIssueDto> Skipped { get; set; } = new();
        public List<ViewPackageUploadIssueDto> Rejected { get; set; } = new();
    }

    public class ViewPackageUploadIssueDto
    {
        public string RelativePath { get; set; } = String.Empty;
        public string? Reason { get; set; }
    }
}
