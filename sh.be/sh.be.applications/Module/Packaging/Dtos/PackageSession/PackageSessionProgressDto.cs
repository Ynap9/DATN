namespace sh.be.applications.Module.Packaging.Dtos.PackageSession
{
    public class PackageSessionProgressDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = String.Empty;
        public int TotalDocument { get; set; }
        public int DoneDocument { get; set; }
        public int ErrorDocument { get; set; }
        public int WarningDocument { get; set; }
        public DateTime? StartedDate { get; set; }
        public DateTime? FinishedDate { get; set; }
        public string? FailReason { get; set; }
    }
}
