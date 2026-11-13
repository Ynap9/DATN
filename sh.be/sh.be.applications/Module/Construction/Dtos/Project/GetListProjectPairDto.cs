namespace sh.be.applications.Module.Construction.Dtos.Project
{
    public class GetListProjectPairDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = String.Empty;
        public int Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsQaEnabled { get; set; }
        public string? UrlGoogleSheetSync { get; set; }
        public int DocumentTypeId { get; set; }
        public string DocumentTypeName { get; set; } = String.Empty;
        public string ViewerRole { get; set; } = String.Empty;
        public int MemberCount { get; set; }
        public int TotalBox { get; set; }
        public int TotalProfileRange { get; set; }
        public int TotalSubmission { get; set; }
        public int CheckedCount { get; set; }
        public int? QaPassedCount { get; set; }
        public decimal ProgressPercent { get; set; }
        public int? FilledFieldCount { get; set; }
    }
}
