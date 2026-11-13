namespace sh.be.applications.Module.Construction.Dtos.Project
{
    public class GetListProjectMyProgressDto
    {
        public int DocumentTypeId { get; set; }
        public string DocumentTypeName { get; set; } = String.Empty;
        public string ViewerRole { get; set; } = String.Empty;
        public int TotalBox { get; set; }
        public int TotalProfileRange { get; set; }
        public int TotalSubmission { get; set; }
        public int CheckedCount { get; set; }
        public decimal ProgressPercent { get; set; }
        public int? FilledFieldCount { get; set; }
    }
}
