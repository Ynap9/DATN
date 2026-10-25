namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class GetListSubmissionTimelineDto
    {
        public string EventType { get; set; } = String.Empty;
        public int LogId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UserId { get; set; } = String.Empty;
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string Description { get; set; } = String.Empty;
        public int? OldStatusCheck { get; set; }
        public int? NewStatusCheck { get; set; }
        public string? Note { get; set; }
        public int? ChangedDataCount { get; set; }
        public int? NewStatusQa { get; set; }
        public string? Reason { get; set; }
        public List<SubmissionTimelineWrongFieldDto>? WrongFields { get; set; }
        public string? EntryUserId { get; set; }
        public string? EntryUserName { get; set; }
    }
}
