namespace sh.be.applications.Module.Construction.Dtos.QaAssignment
{
    public class GetListQaTicketDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int DocumentTypeId { get; set; }
        public string QaUserId { get; set; } = String.Empty;
        public string? QaUserName { get; set; }
        public string Mode { get; set; } = String.Empty;
        public int DocumentCount { get; set; }
        public int PassedCount { get; set; }
        public int? SampleSize { get; set; }
        public int? MaxFailed { get; set; }
        public int Round { get; set; }
        public int RejectedCount { get; set; }
        public int ReviewedCount { get; set; }
        public int FailedCount { get; set; }
        public string Status { get; set; } = String.Empty;
        public DateTime? CreatedDate { get; set; }
        public DateTime? ConcludedDate { get; set; }
    }
}
